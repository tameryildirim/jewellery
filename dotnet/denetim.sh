#!/usr/bin/env bash

# ═══ KABUK KAPISI (2026-09-23, ölçülmüş arıza) ═════════════════════════════
# Bu betik BASH içindir: shebang bash, CI de './denetim.sh' ile koşar
# (.github/workflows/ci.yml:79). zsh ile koşulduğunda AYNI AĞAÇTA FARKLI KARAR
# veriyor — 23.09'da ölçüldü: bash rc=0 "DENETİM TEMİZ" · zsh rc=1 "6 ihlal".
# Altı zsh bulgusunun ALTISI DA artık üründü (örnek: P-36b GIRDI_AILESI_TAM
# zsh'te 0/15, gerçekte Bilesenler/Form altında 16 dosya var; "bekleyen
# migration" iddiası, 72 kaynak - 4 sıfırbayt = 68 = canlıdaki 68 ile çürüdü).
# Daha kötüsü: 22-23.09 arasında bir "${=VAR}" düzeltmesi bash'te
# "bad substitution" verip ÜÇ KONTROLÜ SESSİZCE ATLATIRKEN yine "TEMİZ" diyordu
# (fail-open). Yanlış kabuk = yanlış karar. Kural: bash değilse KOŞMA.
if [ -z "${BASH_VERSION:-}" ]; then
  printf '%s\n' "DENETİM KOŞULMADI — bu betik BASH içindir; başka bir kabukla çalıştırıldı." >&2
  printf '%s\n' "Doğrusu:  bash $0     (CI de böyle koşar: .github/workflows/ci.yml)" >&2
  exit 2
fi

# ═════════════════════════════════════════════════════════════════════════════
# KİRACI İZOLASYONU CI DENETİMİ — ADIM1-KARAR-2026-09-11.md §5/K7, §7.
#
# Bertan'ın şartı: "Bu tasarımın 'unutulunca sessizce açılan kapı' olmamasının
# tek garantisi bu denetimdir." ADIM1-2 raporu §8/2'de "henüz CI'a bağlanmadı"
# diye açık bırakılmıştı — bu betik o açığı kapatır.
#
# Kullanım:  ./denetim.sh          → çıkış kodu 0 (temiz) / 1 (ihlal)
# CI'da:     dotnet test && ./denetim.sh
#
# Yorum satırları sayılmaz; yalnız KOD aranır.
# ═════════════════════════════════════════════════════════════════════════════
set -uo pipefail

# ═══ KENDİ YOLUNU MUTLAKLAŞTIR (P-30b doğrulayıcı kusuru, 2026-09-23) ═══════
# cd sonrası "$0" göreli kalırsa (kök dizinden "bash dotnet/denetim.sh")
# betiğin KENDİNİ okuyan ölçümleri BOŞ döner ve kapı çağrı dizinine göre
# FARKLI KARAR verir (ölçüldü: kökten rc=1, dotnet/ içinden rc=0).
# Kural: kendi yolunu cd'den ÖNCE mutlakla, ölçümlerde $BETIK_YOLU kullan.
BETIK_YOLU="$(cd "$(dirname "$0")" && pwd)/$(basename "$0")"
if [ ! -r "$BETIK_YOLU" ]; then
  printf '%s\n' "DENETİM KOŞULMADI — betik kendi yolunu çözemedi: $BETIK_YOLU" >&2
  exit 2
fi
cd "$(dirname "$BETIK_YOLU")"
# FAIL-CLOSED YOKLAMA (P-30b): betiğin KENDİNİ okuyan ölçümleri (P02-5
# GREP_INCLUDE_EKSIK, P-29 S-3 beyaz liste) okuma BOŞ dönerse "0 ihlal" sanır.
# Boş okuma "temiz" değil "ölçülemedi"dir → denetim burada KIRAR, sonuç basmaz.
if [ "$(grep -c '^BETIK_YOLU=' "$BETIK_YOLU" 2>/dev/null)" != "1" ]; then
  printf '%s\n' "DENETİM KOŞULMADI — ÖLÇÜLEMEDİ(betik kendi yolundan okunamadı: $BETIK_YOLU)" >&2
  exit 2
fi

API="Kuyumcu.Api"
WEB="Kuyumcu.Web"
HATA=0

# Yalnız kod satırları: /obj/ /bin/ ve // ile başlayan satırlar elenir.
kod_say() {
  grep -rnE "$1" "$API" --include='*.cs' 2>/dev/null \
    | grep -v '/obj/\|/bin/\|/Migrations/' \
    | grep -vE ':[0-9]+:[[:space:]]*(//|///|\*)' \
    | grep -vE "$2" || true
}

denetle() {
  local ad="$1" desen="$2" beklenen="$3" haric="${4:-__HICBIRSEY__}"
  local bulunan; bulunan=$(kod_say "$desen" "$haric" | wc -l | tr -d ' ')
  if [ "$bulunan" = "$beklenen" ]; then
    printf "  ✓ %-46s %s\n" "$ad" "$bulunan"
  else
    printf "  ✗ %-46s %s (beklenen %s)\n" "$ad" "$bulunan" "$beklenen"
    kod_say "$desen" "$haric" | sed "s|.*/$API/|      → |" | cut -c1-100
    HATA=1
  fi
}

echo "═══ Kiracı izolasyonu denetimi ═══"

# IgnoreQueryFilters TOPTAN YASAK. Filtresiz erişim yalnız SistemKapsami üzerinden.
denetle "IgnoreQueryFilters (yasak)" "IgnoreQueryFilters" 0

# Bağlamsız DbContext kurulamaz; tek istisna tasarım zamanı fabrikası.
denetle "new KuyumcuDbContext (yalnız fabrika)" "new KuyumcuDbContext" 1

# Find/FindAsync kiracı filtresini ATLAR — PK ile erişim FirstOrDefaultAsync ile yapılır.
denetle "Find/FindAsync (yasak)" "\.Find\(|\.FindAsync\(" 0

# ExecuteUpdate/Delete SaveChanges'ı atlar → ne yazma denetçisi ne denetim izi görür.
# Beyaz liste: Auth ×4 (kimlik öncesi — 3 token yolu + paket F-2-min davet
# tüketimi; davet IKiraciVarlik DEĞİLDİR, yazma denetçisi ve denetim izi o
# tabloya zaten işlemez), Kur ×1 (kiracılar arası bakım).
# ✅ DOKTRİN UYUŞMAZLIĞI KAPANDI (2026-09-23, P-30b / ARIZA 4): yazılı doktrin
#   "beyaz liste 4" diyordu, betik 5 sayıyordu. Karar KODA BAKILARAK verildi:
#   gerçek sayı 5'tir ve beşinin de meşruiyeti tek tek gerekçelendirildi:
#     AuthModulu.cs:176  davet tüketimi — koşullu atomik UPDATE; yarışı ancak tek
#                        ifade kapatır, Davet IKiraciVarlik DEĞİLDİR
#     AuthModulu.cs:376  son giriş damgası — kimlik öncesi, tek toplu nokta
#     AuthModulu.cs:432  refresh token iptali (hash ile) — kimlik öncesi
#     AuthModulu.cs:495  kullanıcının tüm tokenlarının iptali — kimlik öncesi
#     KurModulu.cs       yetim kur snapshot bakımı — kiracılar arası sistem kapsamı; 17.10 P0-7'den beri
#                        HAM SQL (ExecuteSqlRawAsync, pencere fonksiyonu): günün son kuru + 6 tabloya bağlı
#                        kur korunur, defter yazılmaz. Ham SQL de bu sayaca DAHİLDİR (kaçak yol olmasın).
#     SatirKilidi.cs     SELECT … FOR UPDATE satır kilidi (ham SQL, YAZMAZ — eşzamanlılık kapıları)
#   Belgeler düzeltildi: SRS F-803, tehdit-modeli § Sınır 5, ADR-0004.
#   Sayı "yeşile boyamak" için YÜKSELTİLEMEZ: OlcumKapisiTestleri bu tavanı
#   gerçek çağrı sayısına EŞİTLİYOR — yeni çağrıda hem denetim hem test kırılır.
#   ÖLÇÜM: grep -rnE 'ExecuteUpdateAsync\(|ExecuteDeleteAsync\(|ExecuteSqlRawAsync\(' Kuyumcu.Api \
#            --include='*.cs' | grep -v '/obj/\|/bin/' | wc -l   → 6   (17.10: ham SQL dahil — 4 Auth + Kur bakımı + satır kilidi)
denetle "ExecuteUpdate/Delete (beyaz liste 6)" "ExecuteUpdate|ExecuteDelete|ExecuteSqlRaw" 6

# hareketler defterine yazmanın TEK kapısı HareketFabrikasi.Ekle'dir (nakit payı
# = tevsik kümülatifinin kaynağı; fabrikayı atlayan yazma payı boş bırakır ve
# 30.000 TL haddi sessizce delinir). Doğrudan Add yalnız fabrikanın içinde.
denetle "Hareketler.Add (yalnız HareketFabrikasi)" "Hareketler\.Add\(" 1

# PAKET 31 — pos_tahsilatlari defterine yazmanın TEK kapısı PosDefteri.Ekle'dir.
# Hareketler.Add kuralının POS tarafındaki karşılığı: ikinci bir yazma yolu
# açılırsa komisyon/valör snapshot'ı ve durum türetimi atlanır, POS alacağı
# sessizce yanlış olur. Doğrudan Add yalnız Modules/Pos/PosDefteri.cs içinde.
denetle "PosTahsilatlari.Add (yalnız PosDefteri)" "PosTahsilatlari\\.Add\\(" 1

# PAKET 32 (K4-OK-2) — banka_hareketleri tablosuna YAZAN YOLLARIN SAYISI DONDU.
# Ekstre içe aktarımı (EkstreServisi.Onayla) kendi Add'ini AÇMAZ; BankaServisi
# .HareketEkleIc'i çağırır. Açsaydı MASAK kapısı (paket 41, HareketEkleIc içinde
# iki fazlı) ve ileri tarih kontrolü SESSİZCE atlanır, 185.000 TL eşiği toplu
# onayla delinirdi. Hareketler.Add kuralının (yukarısı) banka tarafındaki karşılığı.
#
# ⚠ SAYI 1 DEĞİL 3'TÜR — DÜRÜST ÖLÇÜM (Anayasa md.1). Paket 32 yazılırken ölçüldü:
#   (a) Modules/Banka/BankaModulu.cs     → HareketEkleIc — MEŞRU VE TEK KAPI
#   (b) Modules/Evrak/EvrakModulu.cs:511 → çek tahsili (paket 29) — DEVRALINAN
#   (c) Modules/Kurulum/AcilisServisi.cs:399 → açılış bakiyesi (T-2) — DEVRALINAN
#   (b) ve (c) paket 32'den ÖNCE vardı; onları HareketEkleIc'e taşımak bu paketin
#   kapsamı DIŞINDADIR (teknik borç TB-32-1). Kuralın koruduğu şey sayının
#   ARTMAMASIDIR: 4. bir yazma yolu açan her paket burada kırmızıya döner.
# ⚠ 3 -> 4 (PAKET P-12, B-42) — GEREKÇELİ VE TEK SEFERLİK:
#   (d) Modules/Satis/StornoServisi.cs → BankaHareketiStorno (TEK STORNO MOTORU).
#   Neden HareketEkleIc'e BAĞLANMADI: o kapı MASAK kümülatifini işletir; ters
#   satır oradan geçseydi 100.000 TL'lik bir hatanın stornosu müşterinin
#   günlük toplamını 200.000 TL gösterir ve HATA DÜZELTİLEMEZ hâle gelirdi
#   (StornoServisi başlığındaki gerekçenin banka karşılığı). Bunun yerine
#   çift sayma StornoSuzgeci.StornoCiftiniDisla(IQueryable<BankaHareket>) ile
#   MasakKapisi.GunlukIslemToplam içinde kapatıldı — yani kuralın KORUDUĞU
#   ŞEY (eşiğin sessizce delinmemesi) başka bir mekanizmayla sağlanıyor.
#   5. bir yazma yolu açan her paket burada yine kırmızıya döner.
# ⚠ 4 -> 5 (PAKET P-19, Y-11) — GEREKÇELİ VE TEK SEFERLİK:
#   (e) Data/HareketFabrikasi.cs → CiftBacakFabrikasi.BacaklariEsle (VİRMANIN
#   İKİ BACAĞINI YAZAN TEK KAPI). Neden BankaServisi.HareketEkleIc'e BAĞLANMADI:
#   o kapı TEK satır yazar ve MASAK kümülatifini işletir. Virman İKİ satırı
#   BİRLİKTE, karşılıklı bağlanmış hâlde yazmak zorundadır — iki ayrı çağrı
#   yapılsaydı araya giren bir istisna yarım virman bırakır ve kasa/banka
#   bakiyesi sessizce şişerdi (P-19'un ta kendisi olan risk). MASAK tarafı
#   etkilenmez: virman bacağı DIŞ işlem değildir, karşı tarafı yoktur.
#   6. bir yazma yolu açan her paket burada yine kırmızıya döner.
denetle "BankaHareketleri.Add (yalnız BankaServisi)" "BankaHareketleri\\.Add\\(" 5

# Vade, borç DOĞURAN veresiyede zorunludur (OdemeIzi.Veresiyeden konumsal
# parametre alır). Vadesiz üretimin TEK meşru yeri borç KAPATAN yoldur; o da
# Etiketten'in içindedir. Sayı artarsa biri vadesiz borç yazıyor demektir.
# ⚠ 2 -> 3 (paket 29 Faz A): ikinci meşru çağrı EvrakServisi.Gir'dir. Müşteriden
#   alınan çek/senet VADESİZ ama borç KAPATAN bir veresiye payı yazar; bu kuralın
#   amacı (satır 60-62) vadesiz borç DOĞURMAYI yakalamaktır, borç kapatmayı değil.
#   ⚠ REDDEDİLEN ALTERNATİF: OdemeIzi.Etiketten("VERESIYE", tutar, null) ile sayıyı
#   2'de tutmak. Denetimi kandırmak denetimin amacını öldürür (Bertan + Mehmet).
#   ⚠ Paket 5 ve 6'nın "VeresiyeKapanisi=2 değişmez" iddiası ARTIK GEÇERSİZDİR;
#   sonraki paketler 3'ü devralır.
#   ⚠ 3 -> 4 (PAKET P-21 / Y-21): dördüncü meşru çağrı EvrakServisi.Ciro'dur.
#   Ciro da VADESİZ ama borç KAPATAN bir veresiye payı yazar — bu kez BİZİM
#   borcumuzu kapatır. Aynı gerekçe (satır 60-62): kural vadesiz borç
#   DOĞURMAYI yakalar, kapatmayı değil. Sayı yine YALNIZ gerekçeyle artar.
denetle "VeresiyeKapanisi (Etiketten + Evrak.Gir + Evrak.Ciro)" "VeresiyeKapanisi\\(" 4

# ── PAKET 27 — DÖVİZ DEFTERİ KAPILARI ───────────────────────────────────────
# ⚠ .mutorj YANILTMA İDDİASI DOĞRULANDI ve YANLIŞ ÇIKTI: kod_say
#   `grep --include='*.cs'` kullanır, `.mutorj` uzantısı bu süzgece TAKILMAZ
#   (ölçüldü: `grep -rn UsdBakiye --include='*.cs'` mutorj dosyalarını SAYMAZ).
#   Bu yüzden ayrıca --exclude gerekmedi; sayaçlar kod_say üzerinden gider.
#
# (1) Üç döviz kolonuna DOĞRUDAN atama TEK yerdedir: DovizBakiye.Ekle.
#     Başka bir yere yazılırsa, bir sonraki para biriminde orası unutulur.
#     BEKLENEN İHLAL ÇIKTISI:
#       ✗ Doviz kolonuna dogrudan atama (3 olmalı)     4 (beklenen 3)
DOVIZ_ATAMA=$(kod_say "UsdBakiye[ ]*[+-]?=|EurBakiye[ ]*[+-]?=|GbpBakiye[ ]*[+-]?=" "__HICBIRSEY__")
DOVIZ_ATAMA_SAY=$(printf '%s' "$DOVIZ_ATAMA" | grep -c . || true)
DOVIZ_ATAMA_DISI=$(printf '%s' "$DOVIZ_ATAMA" | grep -vc "Modules/Cari/DovizBakiye.cs" || true)
if [ "$DOVIZ_ATAMA_SAY" = 3 ] && [ "$DOVIZ_ATAMA_DISI" = 0 ]; then
  printf "  ✓ %-46s %s\n" "Doviz kolonu atama (yalnız DovizBakiye)" "$DOVIZ_ATAMA_SAY"
else
  printf "  ✗ %-46s %s (beklenen 3, yalnız DovizBakiye.cs)\n" \
    "Doviz kolonu atama (yalnız DovizBakiye)" "$DOVIZ_ATAMA_SAY"
  printf '%s\n' "$DOVIZ_ATAMA" | sed "s|.*/$API/|      → |" | cut -c1-100
  HATA=1
fi

# (2) DOVIZ_ hareket türü dizgesi YALNIZ iki dosyada geçer: türü TANIMLAYAN
#     DovizBakiye.cs, tür listesini tutan Entities.cs, MASAK kapı listesi ve
#     düzeltme politikası haritası. Başka bir modül bu dizgeyi elle yazarsa
#     ikinci bir döviz yazma yolu doğmuş demektir (defter tekeli kırılır).
DOVIZ_TUR=$(kod_say "DOVIZ_VERESIYE|DOVIZ_TAHSILAT" "__HICBIRSEY__" | cut -d: -f1 | sort -u)
DOVIZ_TUR_SAY=$(printf '%s\n' "$DOVIZ_TUR" | grep -c . || true)
if [ "$DOVIZ_TUR_SAY" = 4 ]; then
  printf "  ✓ %-46s %s\n" "DOVIZ_ turu yazan dosya (sabit 4)" "$DOVIZ_TUR_SAY"
else
  printf "  ✗ %-46s %s (beklenen 4)\n" "DOVIZ_ turu yazan dosya (sabit 4)" "$DOVIZ_TUR_SAY"
  printf '%s\n' "$DOVIZ_TUR" | sed "s|.*/$API/|      → |" | cut -c1-100
  HATA=1
fi

# ── PAKET 20 — MADEN BÜTÜNLÜĞÜ KAPILARI ─────────────────────────────────────
# (1) Ayar listesi TEK KAYNAK: Kuyumcu.Core Ayarlar.Tumu. Desen DÖRT DTO'da
#     elle yazılıydı; yeni bir ayar (çelik) eklenince biri unutuluyor ve
#     "API'de var, uçta yok" sapması doğuyordu. Elle yazılmış desen = 0.
denetle "Elle yazılmış ayar deseni (yasak)" '"K8\|K14' 0

# (2) Maden kapısı deftere yazan TEK yolda çağrılır. Başka bir yerden
#     çağrılırsa "kapıdan geçtim" sanılan ikinci bir yazma yolu doğar;
#     hiç çağrılmazsa madensiz has satırı sessizce ALTIN'a karışır (K20-1).
MADEN_KAPISI=$(kod_say "MadenKapisi\.HareketDogrula" "__HICBIRSEY__" || true)
MADEN_KAPISI_SAY=$(printf '%s' "$MADEN_KAPISI" | grep -c . || true)
MADEN_KAPISI_DISI=$(printf '%s' "$MADEN_KAPISI" | grep -vc "Data/HareketFabrikasi.cs" || true)
if [ "$MADEN_KAPISI_SAY" = 1 ] && [ "$MADEN_KAPISI_DISI" = 0 ]; then
  printf "  ✓ %-46s %s\n" "MadenKapisi.HareketDogrula (yalnız fabrika)" "$MADEN_KAPISI_SAY"
else
  printf "  ✗ %-46s %s (beklenen 1, yalnız HareketFabrikasi.cs)\n" \
    "MadenKapisi.HareketDogrula" "$MADEN_KAPISI_SAY"
  printf '%s\n' "$MADEN_KAPISI" | sed "s|.*/$API/|      → |" | cut -c1-100
  HATA=1
fi

# (3) HasHesap.ToHasGram maden kapısını atlayan sahte has üretmesin: doğrudan
#     çağrı BEYAZ LİSTEYLE sınırlıdır (ürün kartı · emanet · hurda/takas).
denetle "HasHesap.ToHasGram (beyaz liste 4)" "HasHesap\.ToHasGram\(" 4

# ── VEZNE TEK YAZMA YOLU KAPISI (paket 19 Faz A, R10) ────────────────────────
# Vezne modülü deftere KENDİ yazmaz: hareketler ve kasa_hareketleri satırları
# YALNIZ SatisServisi.SatisFisi üzerinden doğar. Vezne kendi defterini yazmaya
# kalkarsa tevsik/MASAK/gün kilidi kapıları ATLANIR ve aynı satış iki yoldan
# yazılabilir hâle gelir.
#   BEKLENEN İHLAL ÇIKTISI:
#     ✗ Vezne/ altında defter yazımı (0 olmalı)      1 (beklenen 0)
VEZNE_DEFTER=$(kod_say "HareketFabrikasi\.Ekle|Hareketler\.Add\(|KasaHareketleri\.Add\(" "__HICBIRSEY__" \
  | grep -cE "(^|/)$API/Modules/Vezne/" || true)
if [ "$VEZNE_DEFTER" = 0 ]; then
  printf "  ✓ %-46s %s\n" "Vezne/ altında defter yazımı (0 olmalı)" "$VEZNE_DEFTER"
else
  printf "  ✗ %-46s %s (beklenen 0)\n" "Vezne/ altında defter yazımı (0 olmalı)" "$VEZNE_DEFTER"
  kod_say "HareketFabrikasi\.Ekle|Hareketler\.Add\(|KasaHareketleri\.Add\(" "__HICBIRSEY__" \
    | grep -E "(^|/)$API/Modules/Vezne/" | sed "s|.*/$API/|      → |" | cut -c1-100
  HATA=1
fi

# Vezne tahsili SatisFisi'yi ÇAĞIRMALIDIR: çağrı kaybolursa yukarıdaki sayaç
# yine 0 kalır ama fiş deftere hiç yazılmaz (sessiz kayıp).
VEZNE_SATIS=$(grep -c "satis\.SatisFisi(" "$API/Modules/Vezne/VezneModulu.cs" 2>/dev/null || true)
if [ "$VEZNE_SATIS" -ge 1 ]; then
  printf "  ✓ %-46s %s\n" "Vezne → SatisFisi çağrısı (>=1)" "$VEZNE_SATIS"
else
  printf "  ✗ %-46s %s (>=1 olmalı)\n" "Vezne → SatisFisi çağrısı" "$VEZNE_SATIS"; HATA=1
fi

# ── ZAMANLANMIŞ İŞ ÇATISI KAPILARI (paket 6 Faz A) ───────────────────────────
# 1) ADR-0008 KAPISI: Isler/ ağacı saklama süresi işini UYGULAMAZ, yalnız
#    Onizleme'yi okur. "Uygula(" geçerse kişisel veri kullanıcının haberi
#    olmadan maskelenmeye başlardı.
#    BEKLENEN İHLAL ÇIKTISI:
#      ✗ Isler/ altında Uygula( (0 olmalı)            1 (beklenen 0)
#    PAKET YASAL-3 (§ 4.3, 2026-10-01): ADR-0008 güncellendi — maskeleme artık
#    KENDİLİĞİNDEN (30 gün önceden SAHİP bildirimi + gerekçeli bekletme; varsayılan
#    Kuru=true). Sayaç 0 → TAM 1: çağrı YALNIZ Isler/Isler/SaklamaUygulaIsi.cs'te,
#    başka Isler/ dosyasında 0. İkizi: IsCatisiSafTestleri M6-6.
ISLER_UYGULA=$(kod_say "Uygula\(" "__HICBIRSEY__" | grep -cE "(^|/)$API/Modules/Isler/")
ISLER_UYGULA_DISI=$(kod_say "Uygula\(" "__HICBIRSEY__" | grep -E "(^|/)$API/Modules/Isler/" | grep -vc "Isler/Isler/SaklamaUygulaIsi.cs" || true)
if [ "$ISLER_UYGULA" = 1 ] && [ "$ISLER_UYGULA_DISI" = 0 ]; then
  printf "  ✓ %-46s %s\n" "Isler/ altında Uygula( (1 — SaklamaUygulaIsi)" "$ISLER_UYGULA"
else
  printf "  ✗ %-46s %s (beklenen 1, yalnız SaklamaUygulaIsi.cs)\n" "Isler/ altında Uygula( (1 olmalı)" "$ISLER_UYGULA"
  kod_say "Uygula\(" "__HICBIRSEY__" | grep -E "(^|/)$API/Modules/Isler/" | sed "s|.*/$API/|      → |" | cut -c1-100
  HATA=1
fi

# 2) DEFTERE DOKUNMA YASAĞI: iş çatısı hareket/kasa defterine YAZMAZ.
ISLER_DEFTER=$(kod_say "HareketFabrikasi|Hareketler\.Add\(|KasaHareketleri\.Add\(" "__HICBIRSEY__" \
  | grep -cE "(^|/)$API/Modules/Isler/")
if [ "$ISLER_DEFTER" = 0 ]; then
  printf "  ✓ %-46s %s\n" "Isler/ defter yazımı (0 olmalı)" "$ISLER_DEFTER"
else
  printf "  ✗ %-46s %s (beklenen 0)\n" "Isler/ defter yazımı (0 olmalı)" "$ISLER_DEFTER"; HATA=1
fi

# ── İÇE AKTARIM KENDİ YAZMA YOLUNU AÇMAZ (paket 47 · Bertan + Mehmet) ───────
# FAIL-CLOSED 3: Modules/Iceaktarim/ altında kendi Add'i OLAMAZ. Satır uygulaması
# YALNIZ mevcut servisler (CariServisi.Olustur / UrunServisi.Olustur) üzerinden
# yürür; kendi Add'i olsaydı alan şifreleme, cari bakiye satırı, barkod üretimi,
# TasKapisi ve MadenKapisi SESSİZCE atlanırdı.
#   BEKLENEN İHLAL ÇIKTISI:
#     ✗ Iceaktarim/ altında dogrudan Add (0 olmalı)  1 (beklenen 0)
ICE_ADD=$(kod_say "Cariler\.Add\(|Urunler\.Add\(|Hareketler\.Add\(|KasaHareketleri\.Add\(|HareketFabrikasi" "__HICBIRSEY__" \
  | grep -cE "(^|/)$API/Modules/Iceaktarim/" || true)
if [ "$ICE_ADD" = 0 ]; then
  printf "  ✓ %-46s %s\n" "Iceaktarim/ altında dogrudan Add (0 olmalı)" "$ICE_ADD"
else
  printf "  ✗ %-46s %s (beklenen 0)\n" "Iceaktarim/ altında dogrudan Add (0 olmalı)" "$ICE_ADD"
  kod_say "Cariler\.Add\(|Urunler\.Add\(|Hareketler\.Add\(|KasaHareketleri\.Add\(|HareketFabrikasi" "__HICBIRSEY__" \
    | grep -E "(^|/)$API/Modules/Iceaktarim/" | sed "s|.*/$API/|      → |" | cut -c1-100
  HATA=1
fi

# İçe aktarım MEVCUT SERVİSİ ÇAĞIRMALIDIR: yukarıdaki sayaç 0 kalsa bile çağrı
# kaybolursa hiçbir kart doğmaz (sessiz kayıp).
ICE_SERVIS=$(grep -c "servisler\.Cari\.Olustur\|servisler\.Urun\.Olustur" \
  "$API/Modules/Iceaktarim/Kaynaklar.cs" 2>/dev/null || true)
if [ "$ICE_SERVIS" -ge 2 ]; then
  printf "  ✓ %-46s %s\n" "İçe aktarım → mevcut servis çağrısı (>=2)" "$ICE_SERVIS"
else
  printf "  ✗ %-46s %s (>=2 olmalı)\n" "İçe aktarım → mevcut servis çağrısı" "$ICE_SERVIS"; HATA=1
fi

# ── KURULUM KENDİ DEFTER YOLUNU AÇMAZ (paket T-2 · Bertan + Mehmet) ─────────
# FAIL-CLOSED 1'İN GERÇEK KARŞILIĞI: Sabitler.HareketTurleri dizisine "ACILIS"
# eklemek kodda ZORLANMAZ (Entities.cs'in kendi yorumu). Gerçek kapı budur:
# Modules/Kurulum/ altında `Hareketler.Add` / `Cariler.Add` / `Urunler.Add`
# OLAMAZ. Defter bacağı HareketFabrikasi.Ekle ile, cari bacağı CariServisi ile
# yazılır; kendi Add'imiz olsaydı bakiye satırı, döviz bacağı ve denetim izi
# sessizce atlanırdı.
#   BEKLENEN İHLAL ÇIKTISI:
#     ✗ Kurulum/ altında dogrudan Add (0 olmalı)     1 (beklenen 0)
KUR_ADD=$(kod_say "Hareketler\.Add\(|Cariler\.Add\(|Urunler\.Add\(" "__HICBIRSEY__" \
  | grep -cE "(^|/)$API/Modules/Kurulum/" || true)
if [ "$KUR_ADD" = 0 ]; then
  printf "  ✓ %-46s %s\n" "Kurulum/ altında dogrudan Add (0 olmalı)" "$KUR_ADD"
else
  printf "  ✗ %-46s %s (beklenen 0)\n" "Kurulum/ altında dogrudan Add (0 olmalı)" "$KUR_ADD"
  kod_say "Hareketler\.Add\(|Cariler\.Add\(|Urunler\.Add\(" "__HICBIRSEY__" \
    | grep -E "(^|/)$API/Modules/Kurulum/" | sed "s|.*/$API/|      → |" | cut -c1-100
  HATA=1
fi

# Açılış MEVCUT CARİ YOLUNU ÇAĞIRMALIDIR: yukarıdaki sayaç 0 kalsa bile çağrı
# kaybolursa cari açılışı hiç yazılmaz (sessiz kayıp).
KUR_SERVIS=$(grep -c "cariServisi\.AcilisYaz" "$API/Modules/Kurulum/AcilisServisi.cs" 2>/dev/null || true)
if [ "$KUR_SERVIS" -ge 1 ]; then
  printf "  ✓ %-46s %s\n" "Açılış → mevcut cari yolu çağrısı (>=1)" "$KUR_SERVIS"
else
  printf "  ✗ %-46s %s (>=1 olmalı)\n" "Açılış → mevcut cari yolu çağrısı" "$KUR_SERVIS"; HATA=1
fi

# 3) DIŞ KANAL TEKELİ: IBildirimKanali DI kaydı TAM 1 (ADR-0016).
KANAL=$(kod_say "AddSingleton<[A-Za-z.]*IBildirimKanali|AddScoped<[A-Za-z.]*IBildirimKanali|AddTransient<[A-Za-z.]*IBildirimKanali" "__HICBIRSEY__" | wc -l | tr -d ' ')
if [ "$KANAL" = 1 ]; then
  printf "  ✓ %-46s %s\n" "IBildirimKanali DI kaydı (1 olmalı)" "$KANAL"
else
  printf "  ✗ %-46s %s (beklenen 1)\n" "IBildirimKanali DI kaydı (1 olmalı)" "$KANAL"; HATA=1
fi

# Her kiracı varlığı işaretlenmiş olmalı (Tenant hariç).
#
# ⚠ SAYAÇ GERÇEK SAYIYA ÇEKİLDİ (paket UX-1-C4 · 45-API devir (d) + 6-A devir):
#   eşik 20'de kalmıştı, gerçek sayı 40'tı. Karşılaştırma -ge olduğu için kapı
#   kırılmıyordu ama sayaç ANLAMSIZDI: bir varlığın IKiraciVarlik işaretini
#   UNUTMAK (= kiracı filtresiz tablo) sessizce geçiyordu.
#   MFA varlıkları (KullaniciMfa · MfaKurtarmaKodu) dâhildir (15→17 talebi),
#   paket 6 (+2) ve paket 3 (+3) de dâhildir.
#
# ⚠ YENİ VARLIK EKLENİNCE BU SAYI DA ARTIRILIR — ölçüm ancak güncel tutulursa
#   kapıdır. Düşürmek yalnız varlık gerçekten kaldırıldığında meşrudur.
#   Paket 25 (StokYerlesimi) ile 40 -> 41.
#   Paket 47 (IceAktarimOturumu + IceAktarimSatiri) ile 41 -> 43.
#   Paket T-2 (KurulumDurumu + AcilisKalemi) ile 43 -> 45.
#   Paket T-3 (YedekTalebi) ile 45 -> 46. ⚠ YedekKaydi ve YedekTatbikati
#   KASTEN kiracısızdır (sistem yedeği tüm kiracıları kapsar) ve
#   KuyumcuDbContext beyaz listesinde ayrıca sayılır.
#   Paket 29-A (KiymetliEvrak) ile 46 -> 47.
#   Paket 32 (EkstreIthalati + EkstreSatiri + CariIban) ile 47 -> 50.
#   Paket 40-A (CariRiskProfili) ile 50 -> 51.
# ⚠ P-02 (2026-09-18): bu taban BAYATTI — dosyada 51 yazılıydı, gerçek sayı 59.
#   "-ge" kullanıldığı için 8 eksik varlık SESSİZCE geçmişti (kapı değil, temenni).
#   Artık "-eq": varlık eklenince ya da kaldırılınca denetim KIRILIR ve sayı elle
#   güncellenir (ham/olcum-2026-09-18.json → sayaclar.BEKLENEN_VARLIK).
#   ÖLÇÜM KOMUTU (Bertan K7 — taban belgeden kopyalanmaz, ölçülür):
#     grep -c ": IKiraciVarlik" Kuyumcu.Api/Data/Entities.cs        → 22
#     Data/ altındaki diğer *.cs toplamı                            → 37   (B-21'in "37"si BUDUR, toplam değil)
#     toplam                                                        → 59
# PAKET P-19 (Y-12): 59 -> 60 — YENİ VARLIK `GiderKategorisi` (gider_kategorileri).
# IKiraciVarlik uygular, global kiracı süzgeci OnModelCreating'de OTOMATİK kurulur
# (KuyumcuDbContext.KiraciFiltresiKur), (tenant_id, ad) BENZERSİZ indeksi vardır.
# PAKET P-21 (Y-23): 60 -> 61 — YENİ VARLIK `Rezervasyon` (rezervasyonlar).
# IKiraciVarlik + ISubeVarlik uygular; global kiracı VE şube süzgeci
# OnModelCreating'de OTOMATİK kurulur, dört indeksi vardır.
# PAKET P-37 (P-36b kapanışında işlendi): 62 -> 63 — YENİ VARLIK `KullaniciTercihi`
# (kullanici_tercihleri). IKiraciVarlik uygular; global kiracı süzgeci
# OnModelCreating'de OTOMATİK kurulur. Kiracı TEK BAŞINA yetmez: tercih
# KULLANICIYA bağlıdır, her sorgu ayrıca kullanici_id ile kırılır.
# ⚠ Bu satırı P-37 güncellemeliydi, bırakılmıştı; taban P-36b kapanışında
#   TAZE ÖLÇÜLDÜ (belgeden kopyalanmadı):
#   grep -c ": IKiraciVarlik" Kuyumcu.Api/Data/Entities.cs            → 25
#   Data/ altındaki diğer *.cs toplamı                                → 38
#   toplam                                                            → 63
# PAKET P-23 (R-17): 63 -> 64 — YENİ VARLIK `OdemeKalemi` (odeme_kalemleri).
# IKiraciVarlik uygular; global kiracı süzgeci OnModelCreating'de OTOMATİK kurulur,
# üç indeksi vardır (fis_id · hareket_id · arac+olusturuldu). ISubeVarlik DEĞİLDİR:
# kalem, şubesi zaten hareket satırında yazılı olan fişin yanında durur.
# TAZE ÖLÇÜM (2026-09-22, belgeden kopyalanmadı):
#   grep -c ": IKiraciVarlik" Kuyumcu.Api/Data/Entities.cs            → 25
#   Data/ altındaki diğer *.cs toplamı                                → 39
#   toplam                                                            → 64
# PAKET P-26 (Y-34/Y-35): 64 -> 68 — DÖRT YENİ VARLIK (Data/Tedarik.cs):
#   SatinalmaSiparisi · SatinalmaKalemi · Irsaliye · IrsaliyeKalemi.
#   Dördü de IKiraciVarlik; başlık ikisi ayrıca ISubeVarlik (sube_id taşıyor —
#   P-04 şube kapsam denetimi aksi hâlde uygulamayı açtırmaz).
# TAZE ÖLÇÜM (2026-09-22, belgeden kopyalanmadı):
#   grep -c ": IKiraciVarlik" Kuyumcu.Api/Data/Entities.cs            → 25
#   Data/ altındaki diğer *.cs toplamı                                → 43
#   toplam                                                            → 68
# PAKET P-27 (R-1): 68 -> 69 — YENİ VARLIK `KiraciAbonelik` (kiraci_abonelikleri).
#   IKiraciVarlik uygular; global kiracı süzgeci OnModelCreating'de OTOMATİK
#   kurulur, üç indeksi vardır. `AbonelikPlani` SAYILMAZ: kiracı-üstü katalog
#   olarak KuyumcuDbContext beyaz listesinde gerekçesiyle durur.
# ⚠ KAPSAM GENİŞLETİLDİ (P-27 · Anayasa md.1 · TAVAN ARTIRILMADI, DELİK KAPATILDI):
#   sayaç yalnız Data/ altını tarıyordu. P-27 varlıklarını Modules/Abonelik/
#   altına koyunca (plan dosya listesi orayı gösteriyor) varlık sayaca HİÇ
#   GÖRÜNMEDİ — yani "Data dışına koy, sayaçtan kaç" yolu açıktı. Kapsam artık
#   Kuyumcu.Api genelidir (Migrations hariç: Designer dosyaları varlık tanımlamaz).
# TAZE ÖLÇÜM (2026-09-22, belgeden kopyalanmadı):
#   grep -c ": IKiraciVarlik" Kuyumcu.Api/Data/Entities.cs                   → 25
#   Data/ altındaki diğer *.cs toplamı                                       → 43
#   Modules/ altındaki *.cs toplamı (P-27 KiraciAbonelik + P-28 iki varlık)  →  3
#   toplam                                                                   → 71
# TAZE ÖLÇÜM (2026-09-23, P-28; belgeden kopyalanmadı — komut yukarıdadır):
#   P-28 TelemetriOlcumu ve DestekDakikasi ile Modules/ payı 1 → 3, toplam 69 → 71.
# PAKET YASAL-2 (2026-09-30): Data/TevsikIhlali.cs (tevsik_ihlalleri) → Data/ payı 43 → 44, toplam 71 → 72.
# PAKET YASAL-3 (2026-10-01): Data/SaklamaBekletme.cs (saklama_bekletmeleri) + Data/Yaptirim.cs
#   (yaptirim_eslesmeleri) → Data/ payı 44 → 46, toplam 72 → 74. (yaptirim_kayitlari KİRACI-ÜSTÜ,
#   IKiraciVarlik DEĞİL — KuyumcuDbContext kapsam denetiminde gerekçeli beyaz liste maddesi.)
# YASAL-4 (2026-10-01): StopajOdemesi (stopaj_odemeleri) → 74 -> 75
# AUTH-SIFIRLA (2026-10-01): Data/ParolaSifirlamaJetonu.cs (parola_sifirlama_jetonlari) → 75 -> 76
# M4 (2026-10-16): Data/UrunKurali.cs (urun_kurallari — müşteri "fiyat kuralları") → 76 -> 77
# 17.10: Data/CariMahsup.cs (cari_mahsuplar) 77 -> 78; KUR KAYNAKLARI: Data/OzelMatrahKapanisi.cs (ozel_matrah_kapanislari) 78 -> 79
#   (ham/olcum-2026-09-18.json sayaclar.BEKLENEN_VARLIK.deger de 79 — dosya git dışıdır, ana checkout'ta).
BEKLENEN_VARLIK=79
VARLIK=$(grep -c ": IKiraciVarlik" "$API/Data/Entities.cs" || true)
EK=$(grep -rl ": IKiraciVarlik" "$API/Data" --include='*.cs' | grep -v Entities.cs | xargs grep -c ": IKiraciVarlik" 2>/dev/null | awk -F: '{t+=$2} END{print t+0}')
MODUL_VARLIK=$(grep -rn ": IKiraciVarlik" "$API/Modules" --include='*.cs' 2>/dev/null | grep -v '/obj/\|/bin/' | wc -l | tr -d ' ')
TOPLAM=$((VARLIK + EK + MODUL_VARLIK))
if [ "$TOPLAM" -eq "$BEKLENEN_VARLIK" ]; then
  printf "  ✓ %-46s %s\n" "IKiraciVarlik uygulayan varlık" "$TOPLAM"
else
  printf "  ✗ %-46s %s (tam %s olmalı — P-02 -eq kapısı)\n" "IKiraciVarlik uygulayan varlık" "$TOPLAM" "$BEKLENEN_VARLIK"; HATA=1
fi

# İstemci-sunucu sözleşmesi dondurma (S-2, MÜHÜR 5 KOŞUL 3): Pages altındaki elle yazılmış
# yanıt modelleri (record) SAYISI artmamalı. Yeni uçlar Kuyumcu.Core sözleşme tipleri üzerinden
# tüketilir; ortak Contracts projesine taşıma 1.1'e ertelendi (SÖZLEŞME-1).
# Uyumsuz alan adı = çalışma zamanı hatası (Stok modalı NRE, /uyum zarfı — QA turu 2).
# Paket T-3 Faz A: Yedekler.razor 7 yanıt modeli getirdi (122 -> 129); yedek
# uçları YENİ sözleşmedir, mevcut Core tipi yoktur. Ortak Contracts projesine
# taşıma SÖZLEŞME-1 ile 1.1'e ertelendi.
# ⚠ P-02 (2026-09-18) — KAPSAM GENİŞLETİLDİ, TAVAN ARTIRILMADI (PLAN § 1 md.9):
#   eski sayaç yalnız Pages'i sayıyordu; bir record'u Bilesenler/ altına taşımak
#   sayacı düşürüyor, sözleşme borcunu ise yerinde bırakıyordu. Kapsam artık
#   Pages + Bilesenler + Layout. Tavan, o geniş kapsamda BUGÜN ÖLÇÜLEN değerdir.
#   ÖLÇÜM KOMUTU (Bertan K7):
#     grep -rn "record " Kuyumcu.Web/Pages Kuyumcu.Web/Bilesenler Kuyumcu.Web/Layout --include='*.razor' | wc -l
#     → Pages 129 + Bilesenler 11 + Layout 0 = 140   (ham/olcum-2026-09-18.json)
#   ⚠ --include='*.razor' ZORUNLU: Pages altında 5 adet .mutorj artığı var (K-4),
#     include'suz grep onları da sayar (MUTORJ_ARTIK sayacına bak).
#   Tavan YALNIZ AZALIR. Yükseltmek isteyen önce P-03 (Kuyumcu.Contracts) ile taşır.
#   ⬇ P-03 (SÖZLEŞME-1, 2026-09-18): TAVAN 140 -> 3. 137 istemci record'u
#     Kuyumcu.Contracts projesine taşındı (saf yer değiştirme). Kalan 3 istisna
#     Kuyumcu.Tests/SozlesmeYansimaTestleri.cs içinde ADIYLA kilitli. Cırcır: yalnız azalır.
SAYFA_RECORD_TAVAN=3

SAYFA_RECORD=$(grep -rn "record " "$WEB/Pages" "$WEB/Bilesenler" "$WEB/Layout" --include='*.razor' 2>/dev/null | wc -l | tr -d ' ')
if [ "$SAYFA_RECORD" -le "$SAYFA_RECORD_TAVAN" ]; then
  printf "  ✓ %-46s %s (tavan %s)\n" "Pages+Bilesenler+Layout record tanımı" "$SAYFA_RECORD" "$SAYFA_RECORD_TAVAN"
else
  printf "  ✗ %-46s %s (tavan %s — yeni istemci record yazılmaz)\n" "Pages+Bilesenler+Layout record tanımı" "$SAYFA_RECORD" "$SAYFA_RECORD_TAVAN"; HATA=1
fi

# Global sorgu filtresi kurulu mu?
HQF=$(grep -c "HasQueryFilter" "$API/Data/KuyumcuDbContext.cs" || true)
if [ "$HQF" -gt 0 ]; then printf "  ✓ %-46s %s\n" "HasQueryFilter" "$HQF"
else printf "  ✗ %-46s 0 (>0 olmalı)\n" "HasQueryFilter"; HATA=1; fi

echo
echo "═══ Güvenlik yüzeyi ═══"
# Yorum satırları elenir — aksi hâlde "eskiden X vardı" açıklaması ihlal sayılır.
kod_var() {
  grep -rn "$1" $API --include='*.cs' 2>/dev/null \
    | grep -v '/obj/\|/bin/\|/Migrations/' \
    | grep -vE ':[0-9]+:[[:space:]]*(//|///|\*)' | head -1
}

for kontrol in "AddRateLimiter:oran sınırı" "UseHttpsRedirection:HTTPS yönlendirme" \
               "AddResponseCompression:sıkıştırma" "AddHealthChecks:sağlık ucu"; do
  anahtar="${kontrol%%:*}"; ad="${kontrol#*:}"
  if [ -n "$(kod_var "$anahtar")" ]; then printf "  ✓ %-46s var\n" "$ad"
  else printf "  ✗ %-46s YOK\n" "$ad"; HATA=1; fi
done

# ── ARAYÜZ MODU BİR TERCİHTİR, YETKİ DEĞİLDİR (paket UX-3 Faz A · Mehmet K1) ──
# Sunucudaki hiçbir yasal kapı arayuz_modu'na BAKMAZ. Bu iki sayaç o ilkeyi kod
# düzeyinde zorlar; M-UX3-07 mutasyonu (bir kapıya mod koşulu eklemek) buradan
# kırmızıya döner. Data/Migrations/ kod_say tarafından zaten elenir.
#
# 1) "ArayuzMod" YALNIZ şu 6 dosyada geçebilir:
#      Data/Entities.cs · Data/KuyumcuDbContext.cs
#      Modules/Kullanici/ArayuzModu.cs · Modules/Kullanici/KullaniciModulu.cs
#      Modules/Auth/AuthModulu.cs · Modules/Admin/AdminModulu.cs
#    BEKLENEN İHLAL ÇIKTISI:
#      ✗ ArayuzModu (yalnız 6 dosya)                  7 (beklenen 6)
ARAYUZ_DOSYALAR=$(kod_say "ArayuzMod" "__HICBIRSEY__" | cut -d: -f1 | sort -u)
ARAYUZ_SAYI=$(printf '%s\n' "$ARAYUZ_DOSYALAR" | grep -c .)
if [ "$ARAYUZ_SAYI" = 6 ]; then
  printf "  ✓ %-46s %s\n" "ArayuzModu (yalnız 6 dosya)" "$ARAYUZ_SAYI"
else
  printf "  ✗ %-46s %s (beklenen 6)\n" "ArayuzModu (yalnız 6 dosya)" "$ARAYUZ_SAYI"
  printf '%s\n' "" | sed "s|^/|      → |"
  HATA=1
fi

# 2) KAPI VE YETKİ YÜZEYİNDE SIFIR EŞLEŞME: Auth altyapısı (RolFiltresi, claim'ler)
#    ve mevzuat kapıları (Tevsik, Masak, taş kökeni, pırlanta) mod'dan habersiz kalır.
#    BEKLENEN İHLAL ÇIKTISI:
#      ✗ ArayuzModu kapı yüzeyinde (0 olmalı)         1 (beklenen 0)
ARAYUZ_KAPI_DESEN="(^|/)$API/(Auth|Modules/(Tevsik|Masak|Satis|Urun|Yetki|Kasa|Cari|Emanet|Hurda|Fiyat|Sayim|Rapor|Maliyet))/"
ARAYUZ_KAPI=$(kod_say "ArayuzMod" "__HICBIRSEY__" | grep -cE "$ARAYUZ_KAPI_DESEN")
if [ "$ARAYUZ_KAPI" = 0 ]; then
  printf "  ✓ %-46s %s\n" "ArayuzModu kapı yüzeyinde (0 olmalı)" "$ARAYUZ_KAPI"
else
  printf "  ✗ %-46s %s (beklenen 0)\n" "ArayuzModu kapı yüzeyinde (0 olmalı)" "$ARAYUZ_KAPI"
  kod_say "ArayuzMod" "__HICBIRSEY__" | grep -E "$ARAYUZ_KAPI_DESEN" | sed "s|.*/$API/|      → |" | cut -c1-100
  HATA=1
fi


# ── FİNANSAL VARLIKTA HARD DELETE YASAK (paket UX-1 Faz B · MÜHÜR 2 / Mehmet) ─
# Deftere yazan ve belge doğuran modüllerde [HttpDelete] OLMAZ: hareketler,
# kasa/banka hareketleri, faturalar, gider pusulası, emanet, MASAK, sayım,
# gün kapanışı. Düzeltme yolu yalnız PASİFLEŞTİRME ya da STORNO'dur
# (VUK md.219 · ADR-0004). Kasa/banka/reyon DELETE'i bu listede DEĞİLDİR:
# onlar satırı silmez, `aktif=0` yazar ve kendi modüllerinde kalır.
#   BEKLENEN İHLAL ÇIKTISI:
#     ✗ Finansal varlıkta [HttpDelete] (0 olmalı)    1 (beklenen 0)
FINANSAL_DESEN="(^|/)$API/Modules/(Satis|Fatura|Emanet|Hurda|Masak|Sayim|Tevsik|Rapor)/"
FIN_DELETE=$(kod_say "\[HttpDelete" "__HICBIRSEY__" \
  | grep -cE "$FINANSAL_DESEN|\[HttpDelete\(\"[^\"]*(hareket|fatura|kapanis)")
if [ "$FIN_DELETE" = 0 ]; then
  printf "  ✓ %-46s %s\n" "Finansal varlıkta [HttpDelete] (0 olmalı)" "$FIN_DELETE"
else
  printf "  ✗ %-46s %s (beklenen 0)\n" "Finansal varlıkta [HttpDelete] (0 olmalı)" "$FIN_DELETE"
  kod_say "\[HttpDelete" "__HICBIRSEY__" \
    | grep -E "$FINANSAL_DESEN|\[HttpDelete\(\"[^\"]*(hareket|fatura|kapanis)" \
    | sed "s|.*/$API/|      → |" | cut -c1-100
  HATA=1
fi

# CORS açık olmamalı (yorumdaki tarihçe sayılmaz)
if [ -n "$(kod_var 'AllowAnyOrigin')" ]; then
  printf "  ✗ %-46s AllowAnyOrigin açık\n" "CORS"
  kod_var 'AllowAnyOrigin' | sed "s|.*/$API/|      → |"; HATA=1
else printf "  ✓ %-46s beyaz liste\n" "CORS"; fi

echo
echo "═══ Arayüz kapıları (paket UX-2 Faz A2 · K14 + MÜHÜR 2 DoD) ═══"
# Hepsi CIRCIR (ratchet): bugünkü değer TAVANDIR. Azalması serbest, ARTMASI FAIL.
# Taban değerler PAKET-UX-2-A2-uygulama-raporu.md § "Denetim sayaçları"nda.

cirkir() {   # ad · bulunan · tavan
  local ad="$1" bulunan="$2" tavan="$3"
  if [ "$bulunan" -le "$tavan" ]; then
    printf "  ✓ %-46s %s (tavan %s)\n" "$ad" "$bulunan" "$tavan"
  else
    printf "  ✗ %-46s %s (tavan %s — ARTIŞ)\n" "$ad" "$bulunan" "$tavan"; HATA=1
  fi
}

# 1) HAM HEX: app.css'te kâğıt (belge/etiket/print) DIŞINDA sabit renk. Tek
#    palet kaynağı belirtecler.css'tir; ham hex ikinci palet demektir.
#    (Tam ölçüm — bildirim bağlamına bakan — TasarimBelirtecTestleri'ndedir;
#    burası kaba ama hızlı çitidir.)
HAM_HEX=$(grep -o '#[0-9a-fA-F]\{3,8\}' "$WEB/wwwroot/css/app.css" | wc -l | tr -d ' ')
cirkir "app.css ham hex (kâğıt dâhil kaba sayım)" "$HAM_HEX" 25

# 2) SATIR İÇİ style="" — CSP style-src 'unsafe-inline' kaldırma işinin ölçüsü
#    (Faz D kalemi). Yeni satır içi style YAZILMAZ.
INLINE_STYLE=$(grep -rn 'style="' "$WEB/Pages" "$WEB/Bilesenler" "$WEB/Layout" --include='*.razor' 2>/dev/null | wc -l | tr -d ' ')
cirkir "Pages+Bilesenler+Layout satır içi style=" "$INLINE_STYLE" 196

# 3) VENDOR CSS LİSANS BAŞLIĞI — lisanssız vendored kopya kapıya takılır.
VENDOR="$WEB/wwwroot/css/vendor/open-props-easing-golge.css"
if [ -f "$VENDOR" ] && grep -q "MIT" "$VENDOR" && grep -q "open-props" "$VENDOR" \
   && grep -q "Copyright" "$VENDOR"; then
  printf "  ✓ %-46s var\n" "vendor CSS lisans başlığı (MIT+kaynak)"
else
  printf "  ✗ %-46s YOK\n" "vendor CSS lisans başlığı (MIT+kaynak)"; HATA=1
fi

# 4) index.html BETİK SAYISI SABİT — inline script yok, CDN yok. A2'de
#    js/cekmece.js eklendi (5 → 6).
#    PAKET P-37 (P-36b kapanışında işlendi): 6 → 7 — js/siralistesi.js.
#    GEREKÇE: menü sırasını sürükleyerek değiştirme, klavyeyle (Alt+↑/↓) ve
#    işaretçiyle AYNI sonucu vermek zorundadır; HTML5 sürükle-bırak olayları
#    Blazor WASM tarafından tek başına taşınamadığı için ince bir yerel köprü
#    betiği gerekti. Betik KENDİ KÖKÜMÜZDEN gelir (CDN yok, npm yok, satır içi
#    yok) ve CSP'de yeni istisna GEREKTİRMEZ — kilidin amacı budur, sayının
#    kendisi değil. P-37 bu beyaz listeyi güncellemeden bırakmıştı.
#    PAKET P-39 (K6): 7 → 8 — js/secici-yerlesim.js. GEREKÇE: açılır seçici
#    paneli 5 ölçülen vakada görüş alanının TAMAMEN altında açılıyordu
#    (TASARIM-DENETIMI-2026-09-21 § 8.2: `/isletme` 136-471 px, `/hurda`
#    249-407 px). "Yer yoksa yukarı aç" kararı ancak ÇALIŞMA ANINDA ölçülen
#    kutu geometrisiyle verilebilir; CSS bunu tek başına yapamaz. Betik KENDİ
#    KÖKÜMÜZDEN gelir (CDN yok, npm yok, satır içi yok), eval kullanmaz ve
#    CSP'de yeni istisna GEREKTİRMEZ.
#    PAKET KATMAN-1: 8 → 9 — js/katman.js. GEREKÇE: .satir-menu popover'ları
#    (satır "⋯", Satış "Diğer ▾") UA varsayılanıyla ekranın ORTASINDA açılıyordu
#    (betikler/tur: SERBEST-YUZEN). Açıcıya bağlamak çalışma anındaki kutu
#    geometrisini ister; kendi kökümüzden, eval yok, yeni CSP istisnası yok.
#    EMANET-1 keşif B006: 9 → 10 — js/oturum-kilidi.js. GEREKÇE: aynı kullanıcı iki
#    sekmede çalışınca sekmeler aynı eski yenileme jetonunu sunucuya götürüyor, sunucu
#    (doğru olarak) çalıntı sayıp tüm oturumları düşürüyordu. Sekmeler arası tek uçuş
#    tarayıcı kilidi (navigator.locks) ister; kendi kökümüzden, eval yok, yeni CSP istisnası yok.
#    GIRDI-AILESI-2: 10 → 11 — js/girdi-maske.js. GEREKÇE: kurucu 08.10 "telefon gibi alanlara harf
#    yazılamaz (tuş anında + yapıştırma temizliği), tarih 08102026 → 08.10.2026" — tuş/yapıştırma
#    olayını tarayıcıda kesmek ister; belge düzeyinde tek dinleyici, kendi kökümüzden, eval yok, CSP istisnası yok.
BETIK=$(grep -c "<script " "$WEB/wwwroot/index.html" | tr -d ' ')
if [ "$BETIK" = 11 ]; then
  printf "  ✓ %-46s %s\n" "index.html <script> sayısı (sabit 11)" "$BETIK"
else
  printf "  ✗ %-46s %s (beklenen 11)\n" "index.html <script> sayısı (sabit 11)" "$BETIK"; HATA=1
fi
# Satır içi <script> BLOĞU hiç olamaz (CSP'de 'unsafe-inline' yok).
INLINE_BETIK=$(grep -cE "^[[:space:]]*<script>" "$WEB/wwwroot/index.html" | tr -d ' ')
if [ "$INLINE_BETIK" = 0 ]; then
  printf "  ✓ %-46s 0\n" "index.html satır içi <script> (0 olmalı)"
else
  printf "  ✗ %-46s %s (beklenen 0)\n" "index.html satır içi <script>" "$INLINE_BETIK"; HATA=1
fi

# 5) DoD/1 — TIKLANINCA HİÇBİR ŞEY OLMAYAN DÜĞME: ne @onclick ne disabled
#    taşıyan <button>. Tavan bugünkü borçtur; B dalgalarında 0'a iner.
OLU_DUGME=$(grep -rn "<button" "$WEB/Pages" --include='*.razor' 2>/dev/null \
  | grep -v "@onclick" | grep -v "disabled" | wc -l | tr -d ' ')
cirkir "Pages: @onclick'siz ve disabled'sız <button>" "$OLU_DUGME" 71

# 6) DoD/2 — TEK DÜZENLEME KALIBI: düzenleme amaçlı ModalPencere / duzenlenenId.
#    2c sonunda hedef 0; A2 taban değeri yazar (A2 Pages/'e HİÇ dokunmaz).
DUZ_MODAL=$(grep -rn "<ModalPencere" "$WEB/Pages" --include='*.razor' 2>/dev/null | wc -l | tr -d ' ')
DUZ_ID=$(grep -rn "duzenlenenId" "$WEB/Pages" --include='*.razor' 2>/dev/null | wc -l | tr -d ' ')
# Paket T-2 Faz A: Kurulum.razor TEK ModalPencere kullanır ve o kullanım
# DÜZENLEME DEĞİL, YIKICI ONAYDIR (mühürleme = geri dönülemez · açılışı geri
# alma = ters kayıt). DoD EK madde 2 "ModalPencere yalnız yıkıcı onay" diyor;
# tavan bu yüzden 45 -> 46 yükseldi, düzenleme kalıbı sayısı (duzenlenenId)
# DEĞİŞMEDİ.
# Paket T-3 Faz A: Yedekler.razor TEK ModalPencere kullanır ve o kullanım
# DÜZENLEME DEĞİL, YIKICI ONAYDIR (yedek talebini iptal etme). Tavan 46 -> 47;
# duzenlenenId kalıbı DEĞİŞMEDİ (ikinci düzenleme kalıbı açılmadı).
# Paket S-3b: İKİ yeni ModalPencere, ikisi de DoD EK madde 4'ün YIKICI ONAY
# kullanımıdır (düzenleme kalıbı DEĞİL): Kur.razor "manuel kur kaydını iptal et"
# ve GiderPusulalari.razor "gider pusulasını iptal et (VUK md.234)". İkisi de
# gerekçe (≥20 karakter) ister ve geri dönüş yolu ekranda yazılıdır. Düzenleme
# amaçlı ModalPencere sayısı (duzenlenenId) ARTMADI. Tavan 47 -> 49.
# Paket 29 Faz A: Evraklar.razor TEK ModalPencere kullanır ve o kullanım
# DÜZENLEME DEĞİL, YIKICI ONAYDIR (evrakı KARŞILIKSIZ işaretlemek — müşterinin
# borcu tam tutarıyla geri doğar ve deftere yeni satır yazılır). Gerekçe
# (≥20 karakter) ister, geri dönüş yolu (tahsil) ekranda yazılıdır.
# ⚠ Düzenleme kalıbı ARTMADI: evrak giriş/düzeltme TEK Cekmece'dedir ve
#   "duzenlenenId" sayacı 1'de KALDI. Tavan 49 -> 50.
# ⚠ 50 -> 51 (paket 32): BankaEkstre.razor'daki TEK modal, seçili satırları banka
#   hareketine çeviren GERİ ALINAMAZ işlemin onayıdır — DoD EK 1/4'ün "yıkıcı
#   eylem ortak onay modalı ister" maddesinin ta kendisi. Aynı pakette açılan
#   ekstre YÜKLEME formu ModalPencere DEĞİL Cekmece'dir (tek düzenleme kalıbı);
#   yani bu paket düzenleme modalı sayısını ARTIRMADI.
# Paket 31: /pos ve /pos/tahsilatlar İKİ ModalPencere getirdi (51 -> 53).
# İkisi de YIKICI ONAYDIR (POS cihazını pasife alma · POS tahsilatını iptal
# etme) — DoD EK madde 2'nin ModalPencere'ye izin verdiği tek kullanım.
# Düzenleme kalıbı Cekmece'dir; ikinci bir "duzenlenenId" modal kalıbı AÇILMADI.
# PAKET P-12 (B-42): /banka TEK yeni ModalPencere alır ve o kullanım DÜZENLEME
# DEĞİL, YIKICI ONAYDIR (dekont stornosu — geri alınamaz ters kayıt üretir).
# DoD EK madde 4 bu eylem için ORTAK ONAY MODALINI ZORUNLU tutar; gerekçe
# (≥20 karakter) ister ve kilit sebebi düğmenin yanında yazılıdır.
# ⚠ Düzenleme kalıbı ARTMADI: dekontun alan düzeltmesi SatirIciDuzenle ile
#   yapılır, ikinci bir "duzenlenenId" modal kalıbı AÇILMADI. Tavan 53 -> 54.
# PAKET P-19 (Y-11/Y-12): /gider TEK yeni ModalPencere alır ve o kullanım
# DÜZENLEME DEĞİL, YIKICI ONAYDIR (gider stornosu · virman stornosu — geri
# alınamaz ters kayıt üretir, virmanda İKİ bacak birden döner). DoD EK madde 4
# bu eylem için ortak onay modalını ZORUNLU tutar; gerekçe (≥20 karakter) ister
# ve kilit sebebi düğmenin yanında yazılıdır (#gd-storno-kilit).
# ⚠ Düzenleme kalıbı ARTMADI: gider kategorisinin çok alanlı düzeltmesi
#   Cekmece ile yapılır (#cekmece-gider-kategori); ikinci bir "duzenlenenId"
#   modal kalıbı AÇILMADI. Tavan 54 -> 55.
cirkir "Pages: ModalPencere kullanımı" "$DUZ_MODAL" 55
cirkir "Pages: duzenlenenId kalıbı" "$DUZ_ID" 1

# 7) DoD/3 — TEK DOKUNUŞ: A2 SIFIR sayfa DOM değişikliği paketidir; Pages/
#    dosya sayısı sabittir. Dalga içi "hangi sayfaya dokunuldu" kaydı
#    betikler/kapi.sh çıktısındadır (kabuk burada onu ÜRETEMEZ — o kayıt
#    orkestratörün kapı dosyasında tutulur; bu sayaç yalnız yeni sayfa
#    sızmasını yakalar).
SAYFA_SAYISI=$(ls "$WEB"/Pages/*.razor 2>/dev/null | wc -l | tr -d ' ')
# Paket 19 Faz A: Pages/Vezne.razor EKLENDİ (30 → 31). Tavan mutlak sayı
# olarak ELLE yazılmaz; mevcut değer okunup bir artırıldı.
# Paket 34 Faz A: rapor seti ekranları EKLENDİ (31 → 34):
#   IslemDefteri.razor (R10) · SatisAnalizi.razor (R12+R13+R14) · KasaRaporu.razor (R11)
#   ⚠ Kalan raporlar YENİ SAYFA AÇMAZ: /raporlar'daki Rapor Merkezi ızgarasından
#     açılır (menü şişmesi reddi, paket 34 § riskler).
# Paket 47 Faz A: Pages/VeriIceAktarim.razor EKLENDİ (34 -> 35). İçe aktarım
# mevcut bir ekranın sekmesi OLAMAZ: dışa aktarım ekranı "verini al" vaadidir,
# içe aktarım "verini getir" akışıdır ve üç adımlı kendi durumunu taşır.
# Paket T-2 Faz A: Pages/Kurulum.razor EKLENDİ (35 -> 36). Kuruluş sihirbazı
# mevcut bir ekranın sekmesi OLAMAZ: altı adımlık kendi durumunu taşır, mühür
# sonrası salt okunura döner ve menüde ayrı bir başlangıç noktasıdır.
# Paket T-3 Faz A: Pages/Yedekler.razor EKLENDİ (36 -> 37). Yedek durumu mevcut
# bir ekranın sekmesi OLAMAZ: SAHİP'e özel, sistem geneli (kiracı üstü) bir
# görünümdür ve basit modda hiç görünmez.
# Paket 29 Faz A: Pages/Evraklar.razor ve Pages/EvrakTakvimi.razor EKLENDİ
# (37 -> 39). Çek/senet portföyü mevcut bir ekranın sekmesi OLAMAZ: kendi durum
# makinesini (portföyde · tahsile verildi · tahsil · karşılıksız) taşır ve
# [ModulYetkisi("evrak")] ile ayrı yetkilendirilir. Vade takvimi ayrı sayfadır
# çünkü SATIR DEĞİL GÜN TOPLAMI gösterir (TB-19) ve listeye süzgeçle bağlanır.
# Paket 32 Faz A: Pages/BankaEkstre.razor EKLENDİ (39 -> 40). Ekstre eşleştirme
# mevcut bir ekranın sekmesi OLAMAZ: SAHNE tablosunu gösterir (banka hareketi
# DEĞİL), kendi durum makinesini (YENI · MUKERRER · ONAYLANDI · ATLANDI ·
# HATALI) ve kendi sayfalamasını taşır. Menüye YENİ SATIR EKLENMEDİ; ekran
# /banka'dan ithalat kimliğiyle çağrılır.
# Paket 31: Pos.razor + PosTahsilatlar.razor eklendi (40 -> 42).
# PAKET P-17 (Y-6 · Y-7): Cikti.razor + Vitrin.razor eklendi (42 -> 44).
# Cikti.razor mevcut bir ekranın sekmesi OLAMAZ: yazdırılabilir belge uygulama
# kabuğundan AYRI yerleşim ister (modalde yazdırmak menüyü kâğıda taşır) ve
# DÖRT belge türünü tek şablona indirger — dört ayrı sayfa açmanın alternatifi
# budur, yani bu satır 42 -> 45 yerine 42 -> 44 ile kapanmıştır.
# Vitrin.razor müşteriye dönük panodur: /kur yönetim ekranıdır, pano ise
# vitrindeki TV'de tam ekran durur; aynı sayfada yaşayamazlar.
# PAKET P-19 (Y-11 · Y-12): Gider.razor eklendi (44 -> 45). PLAN § 2 P-19'un
# dosya listesinde ADIYLA yazılıdır. Mevcut bir ekranın sekmesi OLAMAZ: gider
# kategorisi SOYUT bir kayıttır (kasaya ait değil), virman İKİ kasayı/hesabı
# aynı anda ilgilendirir — /kasalar tek kasanın ekranıdır, /banka tek hesabın.
# İkisini de oraya sıkıştırmak "aynı ekranda aynı sorunun iki cevabı" yasağını
# (UX-S22) çiğnerdi.
# PAKET P-20 (Y-10): Hurda.razor eklendi (45 -> 46). PLAN § 2 P-20'nin dosya
# listesinde `Pages/Hurda.razor` ADIYLA yazılıdır. Mevcut bir ekranın sekmesi
# OLAMAZ: /islemler PLAN § 7'de P-15 → P-16 → P-18 TEK EL zincirindedir ve bu
# paket o zincire giremez; ayrıca hurda bakiyesi bir STOK görünümüdür, işlem
# formu değildir (UX-S22: aynı ekranda aynı sorunun iki cevabı olamaz).
# PAKET S1-d: Ekranlar.razor eklendi (46 -> 47). Görev tanımı "YENİ sayfa
# Pages/Ekranlar.razor, rota /ekranlar" der; kurucunun tuvalinde (Ekranlar.dc.html)
# AYRI bir artboard'dır. Mevcut bir ekranın sekmesi OLAMAZ: tam ekran karo
# menüdür (Pusula'nın karo ana menüsü), kenar çubuğu ve üst bar çizmez.
# PAKET YASAL-1: YasalSinirlar.razor eklendi (47 -> 48). Şartname § 4.5 "Yasal
# sınırlar (YENİ, salt okunur)" ekranını ADIYLA ister (§ 10 dosya listesi:
# Pages/YasalSinirlar.razor YENİ). Mevcut bir ekranın sekmesi OLAMAZ: Ayarlar ve
# Uyum başka şeritlerin (KESIF-P*) dosyasıdır; ekran kiracı verisi değil ULUSAL
# rakam gösterir, rol kapısı (SAHİP/MÜDÜR/MUHASEBE) ikisinden de farklıdır.
# PAKET AUTH-SIFIRLA (2026-10-01): SifreSifirla.razor eklendi (48 -> 49). Görev dosya alanı
# "yeni Pages/SifreSifirla.razor" der. Giriş ekranının sekmesi OLAMAZ: e-postadaki bağlantı
# (#jeton parçası) doğrudan bir ROTAYA iner ve oturumsuz açılır; Giris.razor oturum
# açıkken "/"a yönlenir. Kural "yalnız bağlantı" (Giris.razor'a tek <a href>).
# PAKET EKSIK-B1-SATINALMA (2026-10-09): SatinAlma.razor + Irsaliyeler.razor eklendi (49 -> 51).
# Görev dosya alanı "yeni Pages/SatinAlma*.razor, Pages/Irsaliyeler*.razor" der (EKSIK-OZELLIK-DENETIMI
# B1 satır 1-2: "sunucuda 8 + 10 uç var, ekran ve menü yok"). Mevcut bir ekranın sekmesi OLAMAZ:
# Ürünler/Faturalar başka şeritlerin dosyasıdır, rol kapısı (SAHİP/MÜDÜR/MUHASEBE) ikisinden de farklıdır.
# PAKET EKSIK-KUCUK (2026-10-10): VitrinEkrani.razor eklendi (51 -> 52). Görev "vitrin panosu tam ekran
# kabuksuz (/ekranlar kalıbı)" der (EKSIK-OZELLIK-DENETIMI B2 satır 10). Vitrin.razor'un sekmesi OLAMAZ:
# /vitrin yönetim ekranıdır (marj ayarı, MUDUR+), dükkân ekranı ise müşteriye dönük kabuksuz TV sayfasıdır
# (P-17 gerekçesiyle aynı: "vitrindeki TV'de tam ekran durur; aynı sayfada yaşayamazlar").
cirkir "Pages/*.razor dosya sayısı" "$SAYFA_SAYISI" 52

# 8) DoD/4 — SESSİZ FORM YASAĞI: EditForm kullanan sayfada ValidationMessage
#    (ya da alan altı hata metni) bulunmalı. Bugün Pages/ altında EditForm
#    HİÇ yoktur (formlar elle doğrulanıp `.hata-metni` basar); sayaç yeni
#    EditForm'un doğrulama mesajsız girmesini engeller.
EDITFORM=$(grep -rn "<EditForm" "$WEB/Pages" --include='*.razor' 2>/dev/null | wc -l | tr -d ' ')
VALMSG=$(grep -rn "ValidationMessage" "$WEB/Pages" --include='*.razor' 2>/dev/null | wc -l | tr -d ' ')
if [ "$EDITFORM" -le "$VALMSG" ]; then
  printf "  ✓ %-46s %s EditForm / %s ValidationMessage\n" "Pages: doğrulama mesajsız EditForm yok" "$EDITFORM" "$VALMSG"
else
  printf "  ✗ %-46s %s EditForm / %s ValidationMessage\n" "Pages: doğrulama mesajsız EditForm" "$EDITFORM" "$VALMSG"; HATA=1
fi

# 9) K12 + MÜHÜR 6 (K5/K6/K7) — GÜVENLİK BAŞLIĞININ TEK KAYNAĞI Program.cs.
#
#  ESKİ KURAL (K12) nginx'in API ile AYNI CSP'yi göndermesini istiyordu; yani
#  iki kopyayı elle senkron tutmayı. MÜHÜR 6 bunu kökten çözdü: nginx artık
#  statik SUNMAZ (yalnız ters vekil + TLS), bu yüzden başlık TEK KAYNAKTAN —
#  Program.cs'ten — gelir. nginx/IIS'te bir add_header/customHeaders satırı
#  kalırsa başlık ÇİFT KOPYA döner ve iki katman ayrışır (eski conf SAMEORIGIN
#  gönderiyordu, Program.cs DENY). Bu sayaç o geri dönüşü yakalar.
#  HSTS de tek kopyadır (Program.cs UseHsts); 'preload' bu turda EKLENMEZ.
NGINX="../deploy/nginx-kuyumcu.conf"
IIS="../deploy/iis-web.config"
GUVENLIK_BASLIKLARI='X-Frame-Options|X-Content-Type-Options|Content-Security-Policy|Strict-Transport-Security|Referrer-Policy'
if [ -f "$NGINX" ] && [ -f "$IIS" ]; then
  # Yorum satırları sayılmaz: "buraya add_header eklemeyin" ihlal değildir.
  NG_BASLIK=$(grep -vE '^[[:space:]]*#' "$NGINX" \
    | grep -cE "add_header[[:space:]]+($GUVENLIK_BASLIKLARI)" || true)
  IIS_BASLIK=$(grep -cE "<add[[:space:]]+name=\"($GUVENLIK_BASLIKLARI)\"" "$IIS" || true)
  PROG_CSP=$(grep -c "frame-ancestors 'none'" "$API/Program.cs" || true)
  if [ "$NG_BASLIK" = "0" ] && [ "$IIS_BASLIK" = "0" ] && [ "$PROG_CSP" -ge 1 ]; then
    printf "  ✓ %-46s nginx %s / IIS %s kopya\n" "Güvenlik başlığı TEK KAYNAK (Program.cs)" "$NG_BASLIK" "$IIS_BASLIK"
  else
    printf "  ✗ %-46s nginx %s / IIS %s / Program.cs CSP %s\n" "Güvenlik başlığı ÇİFT KOPYA" "$NG_BASLIK" "$IIS_BASLIK" "$PROG_CSP"; HATA=1
  fi
else
  printf "  ✗ %-46s dosya yok\n" "Güvenlik başlığı TEK KAYNAK"; HATA=1
fi

# ── 10) UX-5 — EMOJİ İKON GERİ SIZMASIN ──────────────────────────────────────
#  Emoji her platformda BAŞKA çizilir, rengi CSS ile kontrol edilemez (karanlık
#  temada okunurluğu şansa kalır) ve kâğıda çoğu yazıcıda çıkmaz. Layout/ ve
#  Bilesenler/ altında ikon artık TEK kaynaktan gelir: wwwroot/img/ikon.svg +
#  <Ikon Ad="@Ikonlar.X" />. Bu sayaç eski kalıbın geri dönmesini yakalar.
#  Yorumlar sayılmaz (@* *@, <!-- -->, /* */, //) — "eskiden burada 📊 vardı"
#  açıklaması ihlal değildir. Aynı ölçüm IkonTestleri'nde de vardır.
EMOJI=$(find "$WEB/Layout" "$WEB/Bilesenler" -type f \( -name '*.razor' -o -name '*.cs' \) 2>/dev/null \
  | xargs perl -0777 -CSD -ne '
      s/\@\*.*?\*\@//gs; s/<!--.*?-->//gs; s{/\*.*?\*/}{}gs; s{^\s*//.*$}{}gm;
      while (/([\x{1F300}-\x{1FAFF}\x{2600}-\x{27BF}\x{23E0}-\x{23FF}\x{2B00}-\x{2BFF}])/g) { print "$ARGV\n" }
    ' 2>/dev/null | wc -l | tr -d ' ')
if [ "$EMOJI" = 0 ]; then
  printf "  ✓ %-46s 0\n" "Layout+Bilesenler emoji ikon (0 olmalı)"
else
  printf "  ✗ %-46s %s (beklenen 0)\n" "Layout+Bilesenler emoji ikon" "$EMOJI"; HATA=1
fi

# 11) UX-5 — SPRITE LİSANS BAŞLIĞI (K14): lisanssız/sürümsüz vendored kopya yok.
SPRITE="$WEB/wwwroot/img/ikon.svg"
if [ -f "$SPRITE" ] && grep -q "ISC License" "$SPRITE" && grep -q "Lucide" "$SPRITE" \
   && grep -q "github.com/lucide-icons/lucide" "$SPRITE" && grep -q "Cole Bemis" "$SPRITE"; then
  printf "  ✓ %-46s var\n" "ikon.svg lisans başlığı (ISC+MIT+kaynak)"
else
  printf "  ✗ %-46s YOK\n" "ikon.svg lisans başlığı (ISC+MIT+kaynak)"; HATA=1
fi


# ── 12) T-5 MİGRATION HİJYENİ (kurul tur 2 engel 20 · MÜHÜR 5 KOŞUL 6) ───────
#  Üç sızıntı kapısı. Bugünkü borç AD AD beyaz listededir; SİLME Kurucu kalemidir
#  (KURUCUYA-KARARLAR-2026-09-16.md md.4 — TurkReport kancası `rm`'i bloklar).
#  Beyaz liste ÇÜRÜMEZ: Kurucu silince kalem "artık yok" diye kırmızıya döner ve
#  listeden elle çıkarılır. İkizi: Kuyumcu.Tests/MigrationHijyeniTestleri.cs.
MIG="$API/Data/Migrations"

#  (a) 0 BAYTLIK .cs — yarıda kesilmiş `dotnet ef migrations add` artığı.
#      Bilinenler: 15 Eylül'de dört kez kesilen FiyatListesiVeKampanya çifti
#      (8 dosya). Bunlar __EFMigrationsHistory'de YOKTUR; EF de görmez.
BOS_MIGRATION_BEYAZ="20260915131748_FiyatListesiVeKampanya.cs
20260915131748_FiyatListesiVeKampanya.Designer.cs
20260915132123_FiyatListesiVeKampanya.cs
20260915132123_FiyatListesiVeKampanya.Designer.cs
20260915132303_FiyatListesiVeKampanya.cs
20260915132303_FiyatListesiVeKampanya.Designer.cs
20260915132645_FiyatListesiVeKampanya.cs
20260915132645_FiyatListesiVeKampanya.Designer.cs"

#  (b) GÜDÜK Designer — BuildTargetModel gövdesi yok; o adımda model anlık
#      görüntüsü kayıp (geriye dönük `migrations script` kör kalır).
GUDUK_DESIGNER_BEYAZ="20260915115025_VadeTakibi.Designer.cs"

#  (c) .mutorj — mut.sh mutasyon yedeği; ajanlar bunları CANLI KOD sanıyordu.
#      28.09 16:25 kurucu 16 .mutorj artığının hepsini sildi (0 bayt) → liste BOŞ;
#      bundan sonra her .mutorj kaçaktır.
MUTORJ_BEYAZ=""

SILME_NOTU="Kurucu sildiyse adı denetim.sh § 12 VE MigrationHijyeniTestleri beyaz listesinden çıkarın."

# Beyaz liste dışı kaçak var mı? (liste satır satır; zsh/bash ortak sözdizimi)
icinde_mi() { printf '%s\n' "$2" | grep -qxF "$1"; }

BOS_KACAK=""
for f in "$MIG"/*.cs; do
  [ -s "$f" ] && continue
  ad="$(basename "$f")"
  icinde_mi "$ad" "$BOS_MIGRATION_BEYAZ" || BOS_KACAK="$BOS_KACAK $ad"
done
if [ -z "$BOS_KACAK" ]; then
  printf "  ✓ %-46s 0\n" "Migrations: beyaz liste dışı 0 baytlık .cs"
else
  printf "  ✗ %-46s%s\n" "Migrations: 0 baytlık .cs (beklenen 0)" "$BOS_KACAK"; HATA=1
fi

GUDUK_KACAK=""
for f in "$MIG"/*.Designer.cs; do
  grep -q "BuildTargetModel" "$f" 2>/dev/null && continue
  ad="$(basename "$f")"
  icinde_mi "$ad" "$BOS_MIGRATION_BEYAZ" && continue
  icinde_mi "$ad" "$GUDUK_DESIGNER_BEYAZ" || GUDUK_KACAK="$GUDUK_KACAK $ad"
done
if [ -z "$GUDUK_KACAK" ]; then
  printf "  ✓ %-46s 0\n" "Migrations: BuildTargetModel'siz Designer"
else
  printf "  ✗ %-46s%s\n" "Migrations: güdük Designer (beklenen 0)" "$GUDUK_KACAK"; HATA=1
fi

MUTORJ_KACAK=""
for f in $(find . -name '*.mutorj' -not -path '*/bin/*' -not -path '*/obj/*' | sed 's|^\./||' | sort); do
  icinde_mi "$f" "$MUTORJ_BEYAZ" || MUTORJ_KACAK="$MUTORJ_KACAK $f"
done
if [ -z "$MUTORJ_KACAK" ]; then
  printf "  ✓ %-46s 0\n" "Kaynak ağacı: beyaz liste dışı .mutorj"
else
  printf "  ✗ %-46s%s\n" "Kaynak ağacı: .mutorj artığı (beklenen 0)" "$MUTORJ_KACAK"; HATA=1
fi

# Beyaz liste çürümesi: kalem silindiyse SESSİZ kalmaz (Anayasa md.1).
CURUK=""
for ad in ${BOS_MIGRATION_BEYAZ} ${GUDUK_DESIGNER_BEYAZ}; do
  [ -f "$MIG/$ad" ] || CURUK="$CURUK $ad"
done
for f in ${MUTORJ_BEYAZ}; do
  [ -f "$f" ] || CURUK="$CURUK $f"
done
if [ -z "$CURUK" ]; then
  printf "  ✓ %-46s %s kalem\n" "T-5 beyaz listesi güncel" \
    "$(printf '%s\n%s\n%s\n' "$BOS_MIGRATION_BEYAZ" "$GUDUK_DESIGNER_BEYAZ" "$MUTORJ_BEYAZ" | wc -l | tr -d ' ')"
else
  printf "  ✗ %-46s%s\n" "T-5 beyaz listesi ÇÜRÜDÜ" "$CURUK"
  printf "      → %s\n" "$SILME_NOTU"; HATA=1
fi

# ── PAKET 34 / MÜHÜR 5 (34-Ö) — RAPOR SORGULARINDA MATERYALİZASYON SAYACI ───
# TB-19: rapor uçları satırları BELLEĞE ALMAK yerine SUNUCUDA toplamalıdır.
# Her `ToListAsync`/`ToDictionaryAsync` bir materyalizasyondur; sayı ARTIYORSA
# biri ya yeni bir sınırsız döküm yazmıştır ya da beyaz listeyi güncellemelidir.
#
# Paket 34 Faz A'da SINIRLANDIRILANLAR:
#   • CariYaslandirma  → limit/ofset + toplamSatir (sunucu tarafı 5 toplama)
#   • StokDegeri       → SQL GroupBy + limit/ofset; ürün kartı yalnız sayfa için
#   • Yeni R10–R14     → hepsi GroupBy/Sum ya da Skip/Take ile sınırlı
#
# ⚠ AÇIK BORÇ (Bertan koşulu — dürüstçe yazıldı, gizlenmedi):
#   GunlukOzet, AylikOzet ve PosFaturaMutabakati hâlâ dönemin TÜM hareketlerini
#   belleğe alır. Dönem (gün/ay) ile sınırlıdırlar ama satır sayısıyla DEĞİL.
#   Faz B işidir; teknik borç envanterine TB-19/34-Ö olarak yazılmıştır.
# ── PAKET P-25 / B-8 — ÖLÇÜM KESKİNLEŞTİRİLDİ (2026-09-22) ──────────────────
#  ESKİ ÖLÇÜM (tavan 43): "Rapor + Maliyet altındaki HER ToListAsync/
#  ToDictionaryAsync satırı". Bu sayaç SINIRLI okumayı SINIRSIZ okumadan
#  AYIRMIYORDU: `GroupBy(...).Select(...).ToListAsync()` (kova/özet toplaması,
#  dönen satır sayısı sabit) ile `Where(...).ToListAsync()` (tüm defteri
#  belleğe alan döküm) aynı sayılıyordu. Sonuç: doğru yönde bir değişiklik
#  (toplamayı SQL'e taşımak) sayacı YÜKSELTİYOR, yanlış yönde bir değişiklik
#  (GroupBy'ı kaldırıp satır çekmek) sayacı DÜŞÜRÜYORDU. Sayaç ters çalışıyordu.
#
#  YENİ ÖLÇÜM: bir ifade (`;` ile ayrılmış deyim) `.Take(` ya da `.GroupBy(`
#  / `group … by` içeriyorsa SINIRLI sayılır ve sayılmaz; kalanlar SINIRSIZ
#  dökümdür. Ölçüm komutu:
#    find "$API/Modules/Rapor" "$API/Modules/Maliyet" -name '*.cs' \
#      | xargs perl -0777 -ne 's{^\s*//[^\n]*}{}gm; for my $x (split /;/) {
#          next unless $x =~ /\.ToListAsync\(\)|\.ToDictionaryAsync\(/;
#          next if $x =~ /\.Take\(/ or $x =~ /\.GroupBy\(/ or $x =~ /\bgroup\s.+\sby\s/s;
#          $n++ } END { print $n+0 }'
#
#  ⚠ TAVAN ARTMADI, DÜŞTÜ: eski ölçüyle 43'tü; yeni ölçüyle P-25 ÖNCESİ 28,
#    P-25 SONRASI 22 ölçüldü (GunlukOzet, AylikOzet, PosFaturaMutabakati
#    sunucuda toplamaya geçti). Taban BELGEDEN kopyalanmadı, TAZE ÖLÇÜLDÜ.
# OLCEK-YUK (2026-10-03): 22 → 20 (KarZarar ürün bazlı ürün adları + vade takibi cari kartları kimlik kümesine daraltıldı).
# RAPOR-4 (2026-10-08): 20 → 18 (KarZarar ürün bazlı: dönemin TÜM satış + parti satırı dökümü kalktı; ay ön-toplamı sayfalı okur).
RAPOR_MAT_BEKLENEN=18
RAPOR_MAT=$(find "$API/Modules/Rapor" "$API/Modules/Maliyet" -name '*.cs' 2>/dev/null \
  | grep -v '/obj/\|/bin/\|/Migrations/\|\.mutorj' \
  | xargs perl -0777 -ne '
      s{^\s*//[^\n]*}{}gm;
      for my $x (split /;/) {
        next unless $x =~ /\.ToListAsync\(\)|\.ToDictionaryAsync\(/;
        next if $x =~ /\.Take\(/ or $x =~ /\.GroupBy\(/ or $x =~ /\bgroup\s.+\sby\s/s;
        $n++;
      }
      END { print $n+0 }' 2>/dev/null | tr -d ' ')
RAPOR_MAT=${RAPOR_MAT:-999}
if [ "$RAPOR_MAT" -le "$RAPOR_MAT_BEKLENEN" ]; then
  printf "  ✓ %-46s %s (üst sınır %s)\n" "Sınırsız rapor dökümü (TB-19/B-8)" "$RAPOR_MAT" "$RAPOR_MAT_BEKLENEN"
else
  printf "  ✗ %-46s %s (üst sınır %s)\n" "Sınırsız rapor dökümü (TB-19/B-8)" "$RAPOR_MAT" "$RAPOR_MAT_BEKLENEN"
  printf "      → Yeni sınırsız döküm mü yazıldı? Sunucu tarafı toplamaya çevir ya da beyaz listeyi GEREKÇEYLE büyüt.\n"
  HATA=1
fi

# ── PAKET S-3a — KÜLTÜRSÜZ BİÇİMLENDİRME SAYACI (kurul tur 3 · engel 19) ────
# Arayüz tr-TR kültüründe koşar: `12.5m.ToString("0.###")` "12,5" üretir.
# İnsan için doğru, MAKİNE için ölümcül — SVG yolu çizilmez, number girdisi
# boşalır, sorgu dizesi ve JSON gövdesi sunucuda 400 olur (çizilmeyen pasta
# grafiği bulgusu buradan doğdu). Makine bağlamında `Bicim.Makine(...)`
# kullanılır (Kuyumcu.Web/Servisler/Bicim.cs).
#
# ⚠ DÜRÜST TAVAN (Anayasa md.1): bugünkü kalıntı SAYILDI ve tavan oraya
#   sabitlendi — sayı ARTAMAZ. "= 0" hedefi 34-Ö taramasının işidir; burada
#   uydurma bir sıfır yazılmaz.
# ⚠ P-25 (2026-09-22): tavan 44 → 27 İNDİRİLDİ. `RaporServisi`/`UyumServisi`
#   sorgu dizesi kuran 14 `ToString("yyyy-MM-dd")` çağrısı `Bicim.Makine(...)`
#   ile değiştirildi (makine bağlamı, tek kaynak). Taban taze ölçüldü.
KULTURSUZ_TAVAN=27
# ⚠ `grep -P` TAŞINABİLİR DEĞİL (BSD grep'te yok, sessizce 0 döndürür ve
#   sahte bir "temiz" üretirdi). Negatif ileri bakış yerine iki aşamalı süzme.
KULTURSUZ=$(grep -rnE 'ToString\("[^"]*"\)' \
    "$WEB/Pages" "$WEB/Bilesenler" "$WEB/Servisler" 2>/dev/null \
  | grep -v '/obj/\|/bin/\|\.mutorj' | grep -v 'Culture' | wc -l | tr -d ' ')
if [ "$KULTURSUZ" -le "$KULTURSUZ_TAVAN" ]; then
  printf "  ✓ %-46s %s (tavan %s)\n" "Kültürsüz biçimli ToString (Web)" "$KULTURSUZ" "$KULTURSUZ_TAVAN"
else
  printf "  ✗ %-46s %s (tavan %s — ARTIŞ)\n" "Kültürsüz biçimli ToString (Web)" "$KULTURSUZ" "$KULTURSUZ_TAVAN"
  printf "      → Makine bağlamıysa Bicim.Makine(...) kullan; insan bağlamıysa CultureInfo'yu AÇIKÇA ver.\n"
  HATA=1
fi

# >>> P-25 ═══ ÖLÇEK SAYAÇLARI (B-8 · R-6 · Y-16 · Y-31) ═════════════════════
#
# P-25-1) SINIRSIZ_TOLIST — TÜM modüllerde sınırsız döküm sayısı (AZALAN).
#
#   NEDEN VAR: TB-19 sayacı yalnız Rapor+Maliyet'e bakıyordu. Sınırsız döküm
#   bir rapor kusuru değil, bir MODÜL kusurudur: `Where(...).ToListAsync()`
#   yazan her uç, kiracı büyüdükçe belleği ve yanıt süresini satır sayısıyla
#   doğru orantılı büyütür. k6 yük testinin (R-6) ölçtüğü şey de budur.
#
#   ÖLÇÜM KOMUTU (tavan bundan TAZE ölçülür, belgeden kopyalanmaz):
#     find "$API/Modules" -name '*.cs' ! -name '*.mutorj' \
#       | xargs perl -0777 -ne 's{^\s*//[^\n]*}{}gm; for my $x (split /;/) {
#           next unless $x =~ /\.ToListAsync\(\)|\.ToDictionaryAsync\(/;
#           next if $x =~ /\.Take\(/ or $x =~ /\.GroupBy\(/ or $x =~ /\bgroup\s.+\sby\s/s;
#           $n++ } END { print $n+0 }'
#
#   TABAN: P-25 ÖNCESİ 151, P-25 SONRASI 145 (GunlukOzet + AylikOzet +
#   PosFaturaMutabakati sunucuda toplamaya geçti). Tavan YALNIZ AZALIR.
# PAKET P-26: tavan ARTMADI. Yeni iki modül (Satinalma · Irsaliye) altı yeni
#   ToListAsync getirdi; altısı da .Take(...) ile sınırlandı (kalem sorgularında
#   AzamiKalem=200, irsaliye listesinde AzamiLimit=200) ve kalem sayısı GİRİŞTE
#   de doğrulanıyor — sorgu sessizce kırpmıyor. TAZE ÖLÇÜM 2026-09-22 → 145.
SINIRSIZ_TOLIST_TAVAN=140   # RAPOR-4 (2026-10-08): 142 → 140 (UrunBazli'nin iki sınırsız dökümü kapandı). OLCEK-YUK (2026-10-03): 145 → 142 (üç sınırsız sözlük kimlik kümesine daraltıldı; .Take ile sınırlı)
SINIRSIZ_TOLIST=$(find "$API/Modules" -name '*.cs' ! -name '*.mutorj' 2>/dev/null \
  | grep -v '/obj/\|/bin/\|/Migrations/' \
  | xargs perl -0777 -ne '
      s{^\s*//[^\n]*}{}gm;
      for my $x (split /;/) {
        next unless $x =~ /\.ToListAsync\(\)|\.ToDictionaryAsync\(/;
        next if $x =~ /\.Take\(/ or $x =~ /\.GroupBy\(/ or $x =~ /\bgroup\s.+\sby\s/s;
        $n++;
      }
      END { print $n+0 }' 2>/dev/null | tr -d ' ')
SINIRSIZ_TOLIST=${SINIRSIZ_TOLIST:-9999}
if [ "$SINIRSIZ_TOLIST" -le "$SINIRSIZ_TOLIST_TAVAN" ]; then
  printf "  ✓ %-46s %s (tavan %s)\n" "Sınırsız ToListAsync (Modules · B-8)" "$SINIRSIZ_TOLIST" "$SINIRSIZ_TOLIST_TAVAN"
else
  printf "  ✗ %-46s %s (tavan %s — ARTIŞ)\n" "Sınırsız ToListAsync (Modules · B-8)" "$SINIRSIZ_TOLIST" "$SINIRSIZ_TOLIST_TAVAN"
  printf "      → Sunucuda topla (GroupBy/Sum) ya da Skip/Take ile sayfala. Tavan ARTIRILMAZ.\n"
  HATA=1
fi

# >>> OLCEK-YUK
# TB-19) SINIRSIZ LİSTE — "gerçekten sınırsız" okuma sayacı (AZALAN, tüm Kuyumcu.Api).
#
#   KAYNAK: HAZIRLIK-KURULU-5.md madde 13 ("Take'siz 209 ToListAsync; kaçının gerçekten
#   sınırsız olduğu doğrulanmadı"). PAKET OLCEK-YUK 207 Take'siz deyimi (Migrations hariç)
#   SINIFLANDIRDI (ham/OLCEK-YUK-tolist-siniflandirma.tsv):
#     SAYFALI          .Skip/.Take var (yorumdaki ';' ayrıştırma artığı dahil)
#     DOĞAL:toplama    GroupBy / group … by (dönen satır = grup sayısı)
#     DOĞAL:kimlik     .Contains(...) — sorgu bir kimlik KÜMESİYLE sınırlı
#     DOĞAL:üst-kayıt  tek üst kayda bağlı (FisId == …, FaturaId == …, UrunId == …)
#     DOĞAL:yapılandırma şube/kasa/rol/kategori/reyon/liste gibi kiracı ayarı (onlarca satır)
#     SINIRSIZ         kalan: kiracı büyüdükçe satır sayısı büyüyen okuma → BU SAYAÇ
#   P-25-1 sayacından FARKI: o yalnız Take/GroupBy'a bakar ve Contains ile daraltılmış
#   okumayı da sınırsız sayar; TB-19 yalnız gerçekten sınırsızı sayar ve Common/ + Data/
#   dahil TÜM API'yi tarar. Yorumlar (satır sonu dahil) ayrıştırmadan önce silinir.
#   OLCEK-YUK ölçümü: 29 → 26 (KarZarar ürün bazlı · vade takibi · fiş araması düzeltildi).
#   Tavan YALNIZ AZALIR; yeni sınırsız okuma ya .Take/.Skip ya da kimlik kümesiyle yazılır.
#   t2 (doğrulayıcı DÜŞÜK, 07.10): kural TEK KAYNAĞA taşındı — betikler/yuk/tolist-siniflandir.pl
#   (--say kipi) hem dökümü hem bu sayacı üretir; betik yoksa sayaç 9999 (fail-closed). İnceltme:
#   Contains ancak argümanı …Id/…Hash ise kimlik kümesi; metin araması kimlik sayılmaz. GroupBy
#   anahtarı CariId/UrunId/FisId olan (kimlik kümesiyle daraltılmamış) toplama AYRI sayaçtır:
#   "BÜYÜYEN toplama" (cari/ürün/fiş sayısıyla büyür). Ölçüm t2: sınırsız 26 · büyüyen 1
#   (PozisyonModulu cari×maden bakiyesi, HAVING ≠ 0).
# RAPOR-4 (2026-10-08): 26 → 24 (KarZararServisi.UrunBazli 2 sınırsız döküm → sayfalı ay ön-toplamı).
TB19_SINIRSIZ_TAVAN=24
TB19_BUYUYEN_TAVAN=1
TB19_BETIK="../dokumanlar/09-uygulama-kayitlari/betikler/yuk/tolist-siniflandir.pl"
if [ -f "$TB19_BETIK" ]; then
  TB19_SONUC=$(find "$API" -name '*.cs' ! -name '*.mutorj' 2>/dev/null \
    | grep -v '/obj/\|/bin/\|/Migrations/' \
    | xargs perl "$TB19_BETIK" --say 2>/dev/null | awk '{a+=$1; b+=$2} END {print a+0, b+0}')
fi
TB19_SINIRSIZ=$(printf '%s' "${TB19_SONUC:-9999 9999}" | awk '{print $1}')
TB19_BUYUYEN=$(printf '%s' "${TB19_SONUC:-9999 9999}" | awk '{print $2}')
if [ "$TB19_SINIRSIZ" -le "$TB19_SINIRSIZ_TAVAN" ]; then
  printf "  ✓ %-46s %s (tavan %s)\n" "TB-19: gerçekten sınırsız liste (OLCEK-YUK)" "$TB19_SINIRSIZ" "$TB19_SINIRSIZ_TAVAN"
else
  printf "  ✗ %-46s %s (tavan %s — ARTIŞ)\n" "TB-19: gerçekten sınırsız liste (OLCEK-YUK)" "$TB19_SINIRSIZ" "$TB19_SINIRSIZ_TAVAN"
  printf "      → .Take/.Skip ile sayfala ya da kimlik kümesiyle (Contains) daralt. Tavan ARTIRILMAZ.\n"
  HATA=1
fi
if [ "$TB19_BUYUYEN" -le "$TB19_BUYUYEN_TAVAN" ]; then
  printf "  ✓ %-46s %s (tavan %s)\n" "TB-19: cari/ürün/fiş başına büyüyen toplama" "$TB19_BUYUYEN" "$TB19_BUYUYEN_TAVAN"
else
  printf "  ✗ %-46s %s (tavan %s — ARTIŞ)\n" "TB-19: cari/ürün/fiş başına büyüyen toplama" "$TB19_BUYUYEN" "$TB19_BUYUYEN_TAVAN"
  printf "      → GroupBy(CariId/UrunId/FisId) sonucu .Take ile sınırla ya da kimlik kümesiyle daralt. Tavan ARTIRILMAZ.\n"
  HATA=1
fi
# TB-19c) YÜK BETİKLERİNDE VARSAYILAN PAROLA = 0 (t2, doğrulayıcı DÜŞÜK; SIR-2 kapısı gelene dek burada).
#   betikler/yuk/ altındaki betikler test hesabı parolasını YALNIZ YUK_SIFRE ortam değişkeninden
#   alır; "YUK_SIFRE || '<metin>'" ya da "sifre: '<metin>'" kalıbı = düz metin varsayılan parola.
TB19_YUK_DIZIN="../dokumanlar/09-uygulama-kayitlari/betikler/yuk"
if [ -d "$TB19_YUK_DIZIN" ]; then
  TB19_PAROLA=$(grep -rnE "YUK_SIFRE[[:space:]]*(\|\||\?\?)[[:space:]]*['\"]|sifre:[[:space:]]*['\"][^'\"]+['\"]" "$TB19_YUK_DIZIN" 2>/dev/null | wc -l | tr -d ' ')
else
  TB19_PAROLA=9999
fi
if [ "$TB19_PAROLA" = "0" ]; then
  printf "  ✓ %-46s %s\n" "TB-19c: yük betiklerinde düz metin parola" "$TB19_PAROLA"
else
  printf "  ✗ %-46s %s\n" "TB-19c: yük betiklerinde düz metin parola" "$TB19_PAROLA"
  printf "      → Parola yalnız YUK_SIFRE ortam değişkeninden; yoksa betik durur.\n"
  HATA=1
fi
# <<< OLCEK-YUK

# P-25-2) BEYAZ_LISTE_BOYU — sınırsız okuyan FONKSİYONLARIN AD AD listesi.
#
#   NEDEN SAYI YETMEZ: yalnız sayıya bakan bir tavan, bir yerde bir dökümü
#   kapatıp başka bir yerde yenisini açmayı görmez. Beyaz liste bu yüzden
#   KÜME EŞİTLİĞİ ile sınanır: ölçülen küme ile yazılı küme BİREBİR aynı
#   olmalıdır. Yeni bir ad çıkarsa da, yazılı bir ad kaybolursa da kapı durur
#   (kaybolan ad "artık yok" demektir ve listeden ELLE çıkarılır — liste
#   çürümez). Liste TARİHLİDİR ve yalnız KISALIR (plan § P-25 sayaçları).
#
#   ⚠ Bu liste bir İZİN değil bir BORÇ KÜTÜĞÜDÜR: her satır "burada hâlâ
#     sınırsız okuma var" demektir ve kapanınca listeden çıkar.
#
#   Kapsam: Rapor + Maliyet (raporlama yüzeyi). Ölçüm ve liste 2026-09-22'de
#   P-25 tarafından TAZE alınmıştır.
BEYAZ_LISTE_TARIHI="2026-09-22"
# RAPOR-4 (2026-10-08): 15 → 14 (KarZararServisi.cs:UrunBazli kapandı, listeden çıktı).
BEYAZ_LISTE_BOYU_TAVAN=14
BEYAZ_LISTE="FinansRaporlari.cs:KasaDefteri
KarZararServisi.cs:KurHaritasi
KarZararServisi.cs:Olc
KarZararServisi.cs:StokDegerlemesi
MaliyetMotoru.cs:AcikPartiler
MaliyetMotoru.cs:PartiIade
RaporModulu.cs:CariYaslandirma
RaporModulu.cs:ReyonStok
RaporModulu.cs:StokDegeri
RaporModulu.cs:VadeTakibi
SatisRaporlari.cs:AdHaritasi
SatisRaporlari.cs:IslemDefteri
SatisRaporlari.cs:OdemeAraci
StokYaslandirmaRaporu.cs:Yaslandirma"

BL_OLCULEN=$(find "$API/Modules/Rapor" "$API/Modules/Maliyet" -name '*.cs' 2>/dev/null \
  | grep -v '/obj/\|/bin/\|\.mutorj' \
  | xargs perl -0777 -ne '
      s{^\s*//[^\n]*}{}gm;
      my $src = $_; my $pos = 0;
      for my $x (split /;/) {
        my $i = index($src, $x, $pos); $pos = $i + length($x);
        next unless $x =~ /\.ToListAsync\(\)|\.ToDictionaryAsync\(/;
        next if $x =~ /\.Take\(/ or $x =~ /\.GroupBy\(/ or $x =~ /\bgroup\s.+\sby\s/s;
        my $onu = substr($src, 0, $i); my $fn = "?";
        $fn = $1 while $onu =~ /(?:public|private|internal|static)[^\n(]*?\s(\w+)\s*\(/g;
        my $b = $ARGV; $b =~ s{.*/}{};
        print "$b:$fn\n";
      }' 2>/dev/null | sort -u)

BL_BOYU=$(printf '%s\n' "$BL_OLCULEN" | grep -c . | tr -d ' ')
BL_FARK=$(diff <(printf '%s\n' "$BEYAZ_LISTE" | sort -u) <(printf '%s\n' "$BL_OLCULEN") 2>/dev/null)

if [ -n "$BL_FARK" ]; then
  printf "  ✗ %-46s liste ≠ ölçüm\n" "Sınırsız okuma beyaz listesi ($BEYAZ_LISTE_TARIHI)"
  printf "%s\n" "$BL_FARK" | sed 's/^/      /'
  printf "      → '>' yeni sınırsız okuma (kapat). '<' kapanmış kalem (listeden ÇIKAR).\n"
  HATA=1
elif [ "$BL_BOYU" -le "$BEYAZ_LISTE_BOYU_TAVAN" ]; then
  printf "  ✓ %-46s %s (tavan %s · %s)\n" "Sınırsız okuma beyaz listesi" "$BL_BOYU" "$BEYAZ_LISTE_BOYU_TAVAN" "$BEYAZ_LISTE_TARIHI"
else
  printf "  ✗ %-46s %s (tavan %s — ARTIŞ)\n" "Sınırsız okuma beyaz listesi" "$BL_BOYU" "$BEYAZ_LISTE_BOYU_TAVAN"
  HATA=1
fi

# P-25-3) Y-31 — RAPOR SAYISI KATALOGDAN OKUNUR, BELGEDEN DEĞİL.
#   `FazAAsgari` katalog dizisinin uzunluğundan KÜÇÜK ya da EŞİT olmalıdır ve
#   ikisi de 16'nın altına düşmemelidir (16. rapor = R15 stok yaşlandırma).
KATALOG_SATIR=$(grep -cE '^\s*new\("R[0-9]+",' "$API/Modules/Rapor/RaporKatalogu.cs" 2>/dev/null | tr -d ' ')
KATALOG_ASGARI=$(grep -oE 'FazAAsgari = [0-9]+' "$API/Modules/Rapor/RaporKatalogu.cs" 2>/dev/null | grep -oE '[0-9]+')
if [ "${KATALOG_SATIR:-0}" -ge 16 ] && [ "${KATALOG_ASGARI:-0}" -eq "${KATALOG_SATIR:-0}" ]; then
  printf "  ✓ %-46s %s\n" "Rapor kataloğu (Y-31 · katalogdan sayılır)" "$KATALOG_SATIR"
else
  printf "  ✗ %-46s katalog %s ≠ FazAAsgari %s (asgari 16)\n" \
    "Rapor kataloğu (Y-31)" "${KATALOG_SATIR:-?}" "${KATALOG_ASGARI:-?}"
  HATA=1
fi
# <<< P-25 ═══════════════════════════════════════════════════════════════════

# ── PAKET T-1 (+F-6) — YAYIN KAPILARI ────────────────────────────────────────
#
# T1-1) Açılış denetçisi TAM 1 KEZ ve YALNIZ Program.cs'ten çağrılır.
#   Neden: ikinci bir çağrı noktası = ikinci bir yapılandırma okuma yolu.
#   Biri "tembel" bir yerden çağırırsa (DI fabrikası, middleware) fail-closed
#   kapısı GEÇ çalışır ve varsayılan sırlı uygulama /saglik'a 200 döner —
#   AlanSifreleme'de CANLIDA yaşanan hatanın aynısı (M-T1-05, M-T1-11).
T1_CAGRI=$(kod_say "UretimAcilisDenetimi\.Dogrula\(" "__HICBIRSEY__" | wc -l | tr -d ' ')
T1_CAGRI_PROGRAM=$(kod_say "UretimAcilisDenetimi\.Dogrula\(" "__HICBIRSEY__" | grep -c "Program\.cs" || true)
if [ "$T1_CAGRI" = "1" ] && [ "$T1_CAGRI_PROGRAM" = "1" ]; then
  printf "  ✓ %-46s %s (Program.cs)\n" "T-1: açılış denetçisi tam 1 çağrı" "$T1_CAGRI"
else
  printf "  ✗ %-46s %s çağrı / %s Program.cs\n" "T-1: açılış denetçisi çağrı sayısı" "$T1_CAGRI" "$T1_CAGRI_PROGRAM"
  kod_say "UretimAcilisDenetimi\.Dogrula\(" "__HICBIRSEY__" | sed "s|.*/$API/|      → |" | cut -c1-100
  HATA=1
fi

# T1-2) Kara liste dizgesi ('DEGISTIR') YALNIZ denetçinin kendi dosyasında.
#   Kod içinde başka bir yerde geçmesi, ya ikinci bir kara liste ya da üretime
#   sızmış bir örnek sır demektir. appsettings*.json ŞABLON sayılır ve elenir.
T1_DEGISTIR=$(kod_say "DEGISTIR" "Common/UretimAcilisDenetimi\.cs" | wc -l | tr -d ' ')
if [ "$T1_DEGISTIR" = "0" ]; then
  printf "  ✓ %-46s %s\n" "T-1: 'DEGISTIR' yalnız denetçi dosyasında" "$T1_DEGISTIR"
else
  printf "  ✗ %-46s %s (beklenen 0)\n" "T-1: 'DEGISTIR' kod içinde sızmış" "$T1_DEGISTIR"
  kod_say "DEGISTIR" "Common/UretimAcilisDenetimi\.cs" | sed "s|.*/$API/|      → |" | cut -c1-100
  HATA=1
fi

# T1-3) Uygulama şemayı KENDİ DEĞİŞTİRMEZ (FAIL-CLOSED 5 / M-T1-07).
#   Otomatik Database.Migrate() yedeksiz ve çok süreçli şema değişimi demektir;
#   geri alınamaz. Şema yükseltmesi TEK YOLDAN: doğrulanmış yedek + bundle.
T1_MIGRATE=$(kod_say "Database\.Migrate\(\)|Database\.MigrateAsync\(" "__HICBIRSEY__" | wc -l | tr -d ' ')
if [ "$T1_MIGRATE" = "0" ]; then
  printf "  ✓ %-46s %s\n" "T-1: otomatik Migrate() yok" "$T1_MIGRATE"
else
  printf "  ✗ %-46s %s (beklenen 0)\n" "T-1: otomatik Migrate() AÇILMIŞ" "$T1_MIGRATE"
  kod_say "Database\.Migrate\(\)|Database\.MigrateAsync\(" "__HICBIRSEY__" | sed "s|.*/$API/|      → |" | cut -c1-100
  HATA=1
fi

# T1-4) Üretim şablonu SIRSIZDIR: appsettings.Production.json'da sır anahtarı yok.
T1_URETIM="$API/appsettings.Production.json"
if [ -f "$T1_URETIM" ]; then
  T1_SIR=$(grep -cE '"(Secret|Anahtar|Password|SifrelemeAnahtari)"[[:space:]]*:|password=' "$T1_URETIM" || true)
  T1_CS=$(grep -c '"ConnectionStrings"' "$T1_URETIM" || true)
  if [ "$T1_SIR" = "0" ] && [ "$T1_CS" = "0" ]; then
    printf "  ✓ %-46s sırsız\n" "T-1: appsettings.Production şablonu"
  else
    printf "  ✗ %-46s %s sır anahtarı / %s ConnectionStrings\n" "T-1: üretim şablonunda SIR" "$T1_SIR" "$T1_CS"; HATA=1
  fi
else
  printf "  ✗ %-46s dosya yok\n" "T-1: appsettings.Production şablonu"; HATA=1
fi

# T1-5) Üretim compose'u 3306'yı ASLA yayımlamaz (M-T1-09).
T1_COMPOSE="../docker-compose.uretim.yml"
if [ -f "$T1_COMPOSE" ]; then
  T1_PORT=$(grep -vE '^[[:space:]]*#' "$T1_COMPOSE" | grep -cE '"?[0-9.:]*3306:3306' || true)
  if [ "$T1_PORT" = "0" ]; then
    printf "  ✓ %-46s kapalı\n" "T-1: üretim compose 3306 dış portu"
  else
    printf "  ✗ %-46s %s satır\n" "T-1: üretim compose 3306 DIŞA AÇIK" "$T1_PORT"; HATA=1
  fi
else
  printf "  ✗ %-46s dosya yok\n" "T-1: docker-compose.uretim.yml"; HATA=1
fi

# .gitignore YENİ .mutorj artıklarını dışarıda tutar (mevcutlar zaten izlenmiyor).
if grep -qF '*.mutorj' ../.gitignore 2>/dev/null; then
  printf "  ✓ %-46s var\n" ".gitignore: *.mutorj"
else
  printf "  ✗ %-46s YOK\n" ".gitignore: *.mutorj"; HATA=1
fi

# ── PAKET F-2-min — SİSTEM ANAHTARI SIR DİSİPLİNİ ───────────────────────────
# F2-1) Anahtar karşılaştırması SABİT ZAMANLIDIR. '==' ile karşılaştırma testle
#       yakalanamaz (ikisi de yeşil kalır) — bu yüzden kapı burada sayaçtır.
F2_FTE=$(grep -nE "FixedTimeEquals" "$API/Modules/Sistem/SistemAnahtariKapisi.cs" 2>/dev/null \
  | grep -vE "^[0-9]+:[[:space:]]*(//|///|\*)" | wc -l | tr -d ' ')
if [ "$F2_FTE" -ge 1 ]; then
  printf "  ✓ %-46s %s\n" "F-2-min: sistem anahtarı FixedTimeEquals" "$F2_FTE"
else
  printf "  ✗ %-46s %s (en az 1)\n" "F-2-min: sistem anahtarı FixedTimeEquals" "$F2_FTE"; HATA=1
fi

# F2-2) Anahtarın HAM karşılaştırması ('==' / Equals) kapı dosyasında olmamalı.
# Anahtar karşılaştırmasında string '==' / Equals / SequenceEqual YASAK.
# (Yalnız bayt UZUNLUĞU karşılaştırması serbesttir: 'a.Length == b.Length'.)
F2_ESIT=$(grep -nE "(gelen|beklenen|Anahtari)[[:space:]]*(==|!=)|(==|!=)[[:space:]]*(gelen|beklenen|ayarlar\.Anahtari)|(gelen|beklenen|Anahtari)\.(Equals|SequenceEqual)" \
  "$API/Modules/Sistem/SistemAnahtariKapisi.cs" 2>/dev/null \
  | grep -vE "^[0-9]+:[[:space:]]*(//|///|\*)" | wc -l | tr -d ' ')
if [ "$F2_ESIT" = "0" ]; then
  printf "  ✓ %-46s 0\n" "F-2-min: anahtarda '==' karşılaştırması"
else
  printf "  ✗ %-46s %s (0 olmalı)\n" "F-2-min: anahtarda '==' karşılaştırması" "$F2_ESIT"; HATA=1
fi

# F2-3) YER TUTUCU SIZINTI SAYACI: appsettings.json'daki Sistem:Anahtari
#       DEGISTIR- ile BAŞLAMALIDIR. Gerçek bir anahtar buraya yazılırsa sır
#       kod deposuna girmiş olur (MÜHÜR 3 — tek sır hattı: ortam değişkeni).
F2_AYAR="$API/appsettings.json"
F2_DEGER=$(grep -E '"Anahtari"[[:space:]]*:' "$F2_AYAR" 2>/dev/null | head -1 | sed -E 's/.*:[[:space:]]*"([^"]*)".*/\1/')
if [ -n "$F2_DEGER" ] && [ "${F2_DEGER#DEGISTIR}" != "$F2_DEGER" ]; then
  printf "  ✓ %-46s yer tutucu\n" "F-2-min: appsettings Sistem:Anahtari"
else
  printf "  ✗ %-46s GERÇEK SIR ya da eksik\n" "F-2-min: appsettings Sistem:Anahtari"; HATA=1
fi

# F2-4) Üretim şablonunda Sistem:Anahtari HİÇ yer almaz (ortam değişkeninden gelir).
if [ -f "$API/appsettings.Production.json" ]; then
  F2_URETIM=$(grep -c '"Anahtari"' "$API/appsettings.Production.json" || true)
  if [ "$F2_URETIM" = "0" ]; then
    printf "  ✓ %-46s yok\n" "F-2-min: üretim şablonunda Sistem:Anahtari"
  else
    printf "  ✗ %-46s %s satır\n" "F-2-min: üretim şablonunda Sistem:Anahtari" "$F2_URETIM"; HATA=1
  fi
fi

# F2-5) Kapı muafiyet listesi GENİŞ ÖNEK içermez ('/api/v1' hepsini muaf ederdi).
F2_GENIS=$(grep -nE '"/api/v1"|"/api"' "$API/Modules/Sistem/KiraciDurumKapisi.cs" 2>/dev/null | wc -l | tr -d ' ')
if [ "$F2_GENIS" = "0" ]; then
  printf "  ✓ %-46s 0\n" "F-2-min: kapı muafiyetinde geniş önek"
else
  printf "  ✗ %-46s %s (0 olmalı)\n" "F-2-min: kapı muafiyetinde geniş önek" "$F2_GENIS"; HATA=1
fi

# F2-6) 'SISTEM' ROLÜ AÇILMADI (MÜHÜR 4 / K1): rol hiyerarşisi sözlüğü ve
#       [RolGerekli] listeleri SISTEM içermez.
F2_ROL=$( { grep -nE "SISTEM" "$API/Auth/AktifKullanici.cs" 2>/dev/null; \
            grep -rnE "RolGerekli\\([^)]*SISTEM" "$API" --include='*.cs' 2>/dev/null \
              | grep -v '/obj/\|/bin/'; } | wc -l | tr -d ' ')
if [ "$F2_ROL" = "0" ]; then
  printf "  ✓ %-46s 0\n" "F-2-min: 'SISTEM' rolü (açılmadı)"
else
  printf "  ✗ %-46s %s (0 olmalı)\n" "F-2-min: 'SISTEM' rolü" "$F2_ROL"; HATA=1
fi

# ── PAKET T-3 — YEDEK OTOMASYONU KAPILARI ───────────────────────────────────
#
# T3-1) API SÜREÇ BAŞLATMAZ. "Elle yedek al" bir kuyruk satırıdır; uygulamadan
#   komut çalıştırmak uzaktan komut enjeksiyonu yüzeyidir (Mehmet oyu).
T3_SUREC=$(kod_say "Process\\.Start|ProcessStartInfo" "__HICBIRSEY__" | wc -l | tr -d ' ')
if [ "$T3_SUREC" = "0" ]; then
  printf "  ✓ %-46s %s\n" "T-3: API'de Process.Start" "$T3_SUREC"
else
  printf "  ✗ %-46s %s (beklenen 0)\n" "T-3: API'de Process.Start" "$T3_SUREC"
  kod_say "Process\\.Start|ProcessStartInfo" "__HICBIRSEY__" | sed "s|.*/$API/|      → |" | cut -c1-100
  HATA=1
fi

# T3-2) ŞİFRESİZ YEDEK YAZILMAZ: betiklerde dump çıktısının şifrelenmeden
#   '> *.sql' yönlendirilmesi YASAK (FAIL-CLOSED 2).
T3_DUZSQL=$(grep -nE '>[[:space:]]*"?\$[A-Za-z_]*\.sql"?|>[[:space:]]*[^|]*\.sql[[:space:]]*$' \
              yedek-al-ve-dogrula.sh yedek-ortak.sh 2>/dev/null \
            | grep -vE ':[0-9]+:[[:space:]]*#' | wc -l | tr -d ' ')
if [ "$T3_DUZSQL" = "0" ]; then
  printf "  ✓ %-46s %s\n" "T-3: betikte şifresiz .sql yazımı" "$T3_DUZSQL"
else
  printf "  ✗ %-46s %s (beklenen 0)\n" "T-3: betikte şifresiz .sql yazımı" "$T3_DUZSQL"; HATA=1
fi

# T3-3) PAROLA ARGV'DE GEÇMEZ: '-p"' ya da '--password=' kalıbı yasaktır
#   (kimlik yalnız --defaults-extra-file ile verilir).
T3_ARGV=$(grep -nE -- '-p"\$|--password=' yedek-al-ve-dogrula.sh yedek-ortak.sh 2>/dev/null \
          | grep -vE ':[0-9]+:[[:space:]]*#' | wc -l | tr -d ' ')
if [ "$T3_ARGV" = "0" ]; then
  printf "  ✓ %-46s %s\n" "T-3: betikte argv parolası" "$T3_ARGV"
else
  printf "  ✗ %-46s %s (beklenen 0)\n" "T-3: betikte argv parolası" "$T3_ARGV"; HATA=1
fi

# T3-4) TEK DUMP YOLU (MÜHÜR 6 / K3): mysqldump çağrısı YALNIZ yedek-ortak.sh
#   içindeki dump_akisi() fonksiyonundadır. İkiz yol = doğrulanmamış yol.
T3_DUMP_DOSYA=$(grep -rlE '^[[:space:]]*[^#]*mysqldump' --include='*.sh' . 2>/dev/null | sort -u | wc -l | tr -d ' ')
T3_DUMP_ORTAK=$(grep -cE '^[[:space:]]*[^#]*mysqldump' yedek-ortak.sh 2>/dev/null || true)
if [ "$T3_DUMP_DOSYA" = "1" ] && [ "$T3_DUMP_ORTAK" -ge 1 ]; then
  printf "  ✓ %-46s %s dosya (yedek-ortak.sh)\n" "T-3: dump çağrı noktası" "$T3_DUMP_DOSYA"
else
  printf "  ✗ %-46s %s dosya (beklenen 1)\n" "T-3: dump çağrı noktası" "$T3_DUMP_DOSYA"
  grep -rlE '^[[:space:]]*[^#]*mysqldump' --include='*.sh' . 2>/dev/null | sed 's|^|      → |'
  HATA=1
fi

# T3-5) YEDEK DOSYASI İNDİRME UCU AÇILMAZ: dosya TÜM kiracıların verisini taşır.
T3_INDIR=$(kod_say "yedekler/\\{?id\\}?/indir|yedekler.*indir" "__HICBIRSEY__" | wc -l | tr -d ' ')
if [ "$T3_INDIR" = "0" ]; then
  printf "  ✓ %-46s %s\n" "T-3: yedek indirme ucu (açılmadı)" "$T3_INDIR"
else
  printf "  ✗ %-46s %s (beklenen 0)\n" "T-3: yedek indirme ucu AÇILMIŞ" "$T3_INDIR"; HATA=1
fi

# T3-6) YEDEK ANAHTARI appsettings'e YAZILMAZ (yalnız YOL ve SAYI tutulur).
T3_AYAR=$(grep -nE '"(SifrelemeAnahtari|Anahtar)"[[:space:]]*:' "$API/appsettings.json" 2>/dev/null \
          | grep -c 'Yedek' || true)
if [ "$T3_AYAR" = "0" ]; then
  printf "  ✓ %-46s %s\n" "T-3: appsettings'te yedek anahtarı" "$T3_AYAR"
else
  printf "  ✗ %-46s %s (beklenen 0)\n" "T-3: appsettings'te yedek anahtarı" "$T3_AYAR"; HATA=1
fi

# >>> P-00 — KAPSAM ENVANTERİ / TRİYAJ SAYAÇLARI (cırcır; PLAN § 1 md.1+6, BK-18, BK-33)
#
# P00-1) KAPSAM-ENVANTERI.md VARDIR. Bu belge olmadan hiçbir dalga başlamaz.
P00_ENV="../dokumanlar/09-uygulama-kayitlari/KAPSAM-ENVANTERI.md"
P00_KAY="../dokumanlar/09-uygulama-kayitlari/ham/triyaj-kayit.json"
if [ -s "$P00_ENV" ]; then
  printf "  ✓ %-46s %s\n" "P-00: kapsam envanteri belgesi" "var"
else
  printf "  ✗ %-46s %s (beklenen: var)\n" "P-00: kapsam envanteri belgesi" "YOK"; HATA=1
fi

# P00-2) KIMLIK_KOKENSIZ = 0 — § 2 tablosundaki her kimlik satırının Kaynak hücresi
#   dolu olmalı (`dosya:satır` biçiminde, ters tırnaklı). Ölçüm:
#   grep -cE '^\| [BYRDK]-[0-9]+ \|' ile satır sayısı, içinde ters tırnak olmayanlar kökensiz.
P00_TABLO=$(awk '/^## 2\./,/^## 3\./' "$P00_ENV" 2>/dev/null | grep -E '^\| [BYRDK]-[0-9]+ \|' || true)
P00_SATIR=$(printf '%s\n' "$P00_TABLO" | grep -c . || true)
P00_KOKENSIZ=$(printf '%s\n' "$P00_TABLO" | grep -c -v '`' || true)
if [ "$P00_KOKENSIZ" = "0" ]; then
  printf "  ✓ %-46s %s (kimlik %s)\n" "P-00: KIMLIK_KOKENSIZ" "$P00_KOKENSIZ" "$P00_SATIR"
else
  printf "  ✗ %-46s %s (beklenen 0)\n" "P-00: KIMLIK_KOKENSIZ" "$P00_KOKENSIZ"; HATA=1
fi

# P00-3) ENVANTER KÜÇÜLMEZ (cırcır): 2026-09-18 P-00 ölçümü 172 kimlik. Kimlik sayısı
#   yalnız artabilir; düşmesi "kapsam sessizce daraltıldı" demektir.
# P-38 (2026-09-22): B-71 ve B-72 envantere yazıldı → taban TAZE ölçüldü:
#   awk '/^## 2\./,/^## 3\./' KAPSAM-ENVANTERI.md | grep -cE '^\| [BYRDK]-[0-9]+ \|'  → 174
P00_TABAN=174
if [ "$P00_SATIR" -ge "$P00_TABAN" ]; then
  printf "  ✓ %-46s %s\n" "P-00: envanter kimlik sayısı (>= $P00_TABAN)" "$P00_SATIR"
else
  printf "  ✗ %-46s %s (en az %s)\n" "P-00: envanter kimlik sayısı" "$P00_SATIR" "$P00_TABAN"; HATA=1
fi

# P00-4) BULGU_DAMGASIZ = 0 — triyaj kaydındaki her bulgunun kimliği ve kutusu olmalı
#   (Y-42 triyaj kuralı: damgasız bulgu kapıyı kırar). Ölçüm: JSON'da null alan sayısı.
P00_DAMGASIZ=$(grep -cE '"(kimlik|kutu)": null' "$P00_KAY" 2>/dev/null || true)
if [ -s "$P00_KAY" ] && [ "$P00_DAMGASIZ" = "0" ]; then
  printf "  ✓ %-46s %s\n" "P-00: BULGU_DAMGASIZ" "$P00_DAMGASIZ"
else
  printf "  ✗ %-46s %s (beklenen 0, kayıt dosyası dolu olmalı)\n" "P-00: BULGU_DAMGASIZ" "$P00_DAMGASIZ"; HATA=1
fi

# P00-5) YENI_KIMLIK_DALGA — yalnız RAPORLANIR (kapı kırmaz, kapsam freni girdisi BK-33).
P00_YENI=$(grep -oE '`YENI_KIMLIK_DALGA` \| [0-9]+' "$P00_ENV" 2>/dev/null | grep -oE '[0-9]+$' | head -1)
printf "  i %-46s %s\n" "P-00: YENI_KIMLIK_DALGA (raporlanır)" "${P00_YENI:-?}"
# <<< P-00

# >>> P-01 — SÜRÜM MÜHRÜ SAYAÇLARI (cırcır; PLAN § 2 P-01, § 5 R-9/R-11..R-15/R-19, BK-07)
#
# TEK KAYNAK: ham/surum.json. v1-surum-muhru.js'in ürettiği belge ve ana belgedeki
# § MÜHÜR 2-4 satırları ARŞİVDİR; çelişkide surum.json geçerlidir.
P01_SUR="../dokumanlar/09-uygulama-kayitlari/ham/surum.json"
P01_PLAN="../PLAN-TEK-SEFER-2026-09-18.md"

# P01-1) SURUM_KARAR_BEKLEYEN = 0 — her satır {1.0, 1.1, KAPSAM-DIŞI} kümesinden biri.
#   Ölçüm: toplam '"surum":' satırı eksi küme içindekiler.
#   grep -c '"surum":' ham/surum.json  ·  grep -cE '"surum": "(1\.0|1\.1|KAPSAM-DIŞI)"' ham/surum.json
P01_TOPLAM=$(grep -c '"surum":' "$P01_SUR" 2>/dev/null || true)
P01_GECERLI=$(grep -cE '"surum": "(1\.0|1\.1|KAPSAM-DIŞI)"' "$P01_SUR" 2>/dev/null || true)
P01_BEKLEYEN=$(( P01_TOPLAM - P01_GECERLI ))
if [ -s "$P01_SUR" ] && [ "$P01_BEKLEYEN" = "0" ]; then
  printf "  ✓ %-46s %s (satır %s)\n" "P-01: SURUM_KARAR_BEKLEYEN" "$P01_BEKLEYEN" "$P01_TOPLAM"
else
  printf "  ✗ %-46s %s (beklenen 0; dosya dolu olmalı)\n" "P-01: SURUM_KARAR_BEKLEYEN" "$P01_BEKLEYEN"; HATA=1
fi

# P01-2) MÜHÜR KÜÇÜLMEZ (cırcır): 2026-09-18 P-01 ölçümü 89 satır. Satır sayısı yalnız
#   artabilir; düşmesi "bir paketin sürümü sessizce silindi" demektir.
P01_TABAN=89
if [ "$P01_TOPLAM" -ge "$P01_TABAN" ]; then
  printf "  ✓ %-46s %s\n" "P-01: sürüm satırı (>= $P01_TABAN)" "$P01_TOPLAM"
else
  printf "  ✗ %-46s %s (en az %s)\n" "P-01: sürüm satırı" "$P01_TOPLAM" "$P01_TABAN"; HATA=1
fi

# P01-3) SURUM_PLANSIZ_PAKET = 0 — PLAN § 2'deki her '#### P-xx' bloğunun surum.json'da
#   satırı olmalı (plan ile mühür ayrışamaz).
#   Ölçüm: grep -oE '^#### P-[0-9]{2}' PLAN... | her kimlik için grep -c '"P-xx":' surum.json
P01_EKSIK=0
for P01_ID in $(grep -oE '^#### P-[0-9]{2}' "$P01_PLAN" 2>/dev/null | grep -oE 'P-[0-9]{2}'); do
  grep -q "\"$P01_ID\":" "$P01_SUR" 2>/dev/null || P01_EKSIK=$(( P01_EKSIK + 1 ))
done
if [ "$P01_EKSIK" = "0" ]; then
  printf "  ✓ %-46s %s\n" "P-01: SURUM_PLANSIZ_PAKET" "$P01_EKSIK"
else
  printf "  ✗ %-46s %s (beklenen 0)\n" "P-01: SURUM_PLANSIZ_PAKET" "$P01_EKSIK"; HATA=1
fi

# P01-4) SURUM_IKINCI_KAYNAK = 0 — eski mühür üreticileri "TEK KAYNAK DEĞİLDİR" damgası
#   taşımalı ve surum-kaydet.js küme kontrolünü içermeli (fail-closed birleştirme).
#   Ölçüm: üç dosyada damga/kontrol dizgesi aranır; bulunmayan her biri 1 puan.
P01_IK=0
grep -q 'TEK KAYNAK DEĞİLDİR' "../dokumanlar/02-mimari/SURUM-MUHRU-V1-2026-09-16.md" 2>/dev/null || P01_IK=$(( P01_IK + 1 ))
grep -q 'TEK KAYNAK DEĞİLDİR' "../dokumanlar/09-uygulama-kayitlari/betikler/v1-surum-muhru.js" 2>/dev/null || P01_IK=$(( P01_IK + 1 ))
grep -q "KUME = \['1.0', '1.1', 'KAPSAM-DIŞI'\]" "../dokumanlar/09-uygulama-kayitlari/betikler/surum-kaydet.js" 2>/dev/null || P01_IK=$(( P01_IK + 1 ))
if [ "$P01_IK" = "0" ]; then
  printf "  ✓ %-46s %s\n" "P-01: SURUM_IKINCI_KAYNAK" "$P01_IK"
else
  printf "  ✗ %-46s %s (beklenen 0)\n" "P-01: SURUM_IKINCI_KAYNAK" "$P01_IK"; HATA=1
fi
# <<< P-01

# >>> P-02 — YAYIN DAMGASI + ÖLÇÜM KAPISI + TAZE SAYAÇ TABANLARI
#     (cırcır; PLAN § 2 P-02, kimlikler B-1 · B-2 · B-21 · B-53 · Y-33,
#      bağlı koşullar BK-05, BK-07, BK-08, BK-39, BK-42; riskler R4, R15, R18)
#
# İLKE (PLAN § 1 md.2): buradaki HİÇBİR taban belgeden kopyalanmadı. Her sayacın
# ÖLÇÜM KOMUTU yorum olarak yazılıdır ve tabanların tamamı
# ham/olcum-2026-09-18.json dosyasında sürümlüdür. P02-8 aynı komutları yeniden
# koşup dosyayla karşılaştırır: "taban kopyalandı" durumu fail-closed yakalanır.
P02_KAYIT="../dokumanlar/09-uygulama-kayitlari"
P02_OLCUM="$P02_KAYIT/ham/olcum-2026-09-18.json"
P02_KAPI="$P02_KAYIT/betikler/kapi.sh"
P02_QA="$P02_KAYIT/betikler/qa-turu.js"
P02_SUNUCU="$P02_KAYIT/betikler/qa-sunucu.sh"
P02_LAUNCH="../.claude/launch.json"

p02_ok()  { printf "  ✓ %-46s %s\n" "$1" "$2"; }
p02_kir() { printf "  ✗ %-46s %s\n" "$1" "$2"; HATA=1; }
p02_uyar(){ printf "  ⚠ %-46s %s\n" "$1" "$2"; }

echo
echo "═══ P-02 ölçüm kapısı ═══"

# ── P02-1) ROTA_KAPSAM = 42 (-eq) · Y-33 ─────────────────────────────────────
#   Rota tanımı ile başlık haritası İKİ YÖNLÜ kilitlenir. RotaBaslikTestleri
#   yansımayla aynı şeyi çalışma zamanında sınar; bu sayaç aynı gerçeği
#   DERLEMESİZ ve testsiz de görünür kılar (CI hızlı kapısı).
#   ⚠ --include='*.razor' ZORUNLU (R18): include'suz grep Pages'teki 5 .mutorj
#     artığını da sayar ve 42 yerine 47 gösterir — taslağın "47 razor" hatası budur.
#   ÖLÇÜM:
#     grep -rhE '^@page' Kuyumcu.Web/Pages --include='*.razor' | wc -l          → 42
#     grep -cE '^[[:space:]]*\["/' Kuyumcu.Web/Layout/RotaBasliklari.cs         → 42
# PAKET P-17: 42 → 44 (/vitrin · /cikti/{Tur}/{Id}) — taze ölçüldü:
#   grep -rhE '^@page' Kuyumcu.Web/Pages --include='*.razor' | wc -l  → 44
# PAKET P-19 (Y-11/Y-12): 44 → 45 (/gider) — taze ölçüldü:
#   grep -rhE '^@page' Kuyumcu.Web/Pages --include='*.razor' | wc -l  → 45
# PAKET P-20 (Y-10): 45 → 46 (/hurda) — taze ölçüldü:
#   grep -rhE '^@page' Kuyumcu.Web/Pages --include='*.razor' | wc -l  → 46
# PAKET S1-d: 46 → 47 (/ekranlar — karo menü) — taze ölçüldü:
#   grep -rhE '^@page' Kuyumcu.Web/Pages --include='*.razor' | wc -l  → 47
# PAKET YASAL-1: 47 → 48 (/yasal-sinirlar — salt okunur yasal sınırlar) — taze ölçüldü:
#   grep -rhE '^@page' Kuyumcu.Web/Pages --include='*.razor' | wc -l  → 48
# PAKET AUTH-SIFIRLA: 48 → 49 (/sifre-sifirla — Parolamı unuttum) — taze ölçüldü:
#   grep -rhE '^@page' Kuyumcu.Web/Pages --include='*.razor' | wc -l  → 49
# PAKET EKSIK-B1-SATINALMA: 49 → 51 (/satin-alma · /irsaliyeler) — taze ölçüldü:
#   grep -rhE '^@page' Kuyumcu.Web/Pages --include='*.razor' | wc -l  → 51
# PAKET EKSIK-KUCUK: 51 → 52 (/vitrin/ekran — kabuksuz dükkân ekranı) — taze ölçüldü:
#   grep -rhE '^@page' Kuyumcu.Web/Pages --include='*.razor' | wc -l  → 52
P02_ROTA_BEKLENEN=52
P02_ROTA=$(grep -rhE '^@page' "$WEB/Pages" --include='*.razor' 2>/dev/null | wc -l | tr -d ' ')
P02_HARITA=$(grep -cE '^[[:space:]]*\["/' "$WEB/Layout/RotaBasliklari.cs" 2>/dev/null || true)
if [ "$P02_ROTA" -eq "$P02_ROTA_BEKLENEN" ] && [ "$P02_HARITA" -eq "$P02_ROTA_BEKLENEN" ]; then
  p02_ok "P-02: ROTA_KAPSAM (@page ≡ RotaBasliklari)" "$P02_ROTA"
else
  p02_kir "P-02: ROTA_KAPSAM (@page ≡ RotaBasliklari)" "@page=$P02_ROTA harita=$P02_HARITA (tam $P02_ROTA_BEKLENEN olmalı)"
fi

# ── P02-1b) OLU_IC_BAGLANTI = 0 (PAKET P-D1 · D-b/D-c · demo kapısı) ────────
#   ÖLÇÜLMÜŞ ARIZA (23.09): Pages/Panel.razor href="/islem-defteri" — böyle bir
#   @page YOKTU (gerçeği "/raporlar/islem-defteri"). Uygulamadaki TEK ölü iç
#   bağlantıydı ve cumartesi demosunun 2. ekranındaydı: tıklanınca "Sayfa
#   bulunamadı" çizerdi. P02-1 rota SAYISINI ölçüyor, P-09 rota↔yetki eşleşmesini
#   ölçüyor; hiçbiri "bu bağlantı bir yere gidiyor mu" sorusunu sormuyordu.
#   Bu sayaç o boşluğu kapatır: her düz metin iç bağlantı hedefi @page kümesinde
#   OLMALIDIR. Kapı olmadan aynı hata yarın geri gelir.
#
#   KAPSAM: href="/..." ve NavigateTo("/...") — Pages · Bilesenler · Layout ·
#   Servisler. Değişken/enterpolasyonlu hedefler (href="@(...)") statik olarak
#   çözülemez, sayılmaz; bu sayacın iddiası DÜZ METİN bağlantılarla sınırlıdır.
#   ELENENLER: (1) sorgu dizesi ve çapa soyulur (/gider?sekme=virman → /gider);
#   (2) uzantılı hedefler STATİK VARLIKTIR, rota değildir (/kilavuz.html,
#       /img/ikon.svg#...) — bunların varlığı ayrı bir konudur (S-4 /kilavuz).
#   (3) parametreli rotalar {Ad} biçimine indirgenir.
#   ÖLÇÜM (elle tekrar için):
#     grep -rhoE 'href="/[^"]*"' Kuyumcu.Web/{Pages,Bilesenler,Layout,Servisler} \
#       --include='*.razor' --include='*.cs' | sed -E 's/href="([^"]*)"/\1/'
P02_LINK_KOK="$WEB"
P02_ROTA_KUMESI=$(grep -rhE '^@page' "$WEB/Pages" --include='*.razor' 2>/dev/null \
  | sed -E 's/^@page[[:space:]]+"([^"]*)".*/\1/' \
  | sed -E 's/\{([A-Za-z]+)(:[a-z]+)?\}/{\1}/g' | sort -u)
P02_LINK_HAM=$( { grep -rhoE 'href="/[^"]*"' \
      "$P02_LINK_KOK/Pages" "$P02_LINK_KOK/Bilesenler" "$P02_LINK_KOK/Layout" "$P02_LINK_KOK/Servisler" \
      --include='*.razor' --include='*.cs' 2>/dev/null | sed -E 's/href="([^"]*)"/\1/'
    grep -rhoE 'NavigateTo\("/[^"]*"' \
      "$P02_LINK_KOK/Pages" "$P02_LINK_KOK/Bilesenler" "$P02_LINK_KOK/Layout" "$P02_LINK_KOK/Servisler" \
      --include='*.razor' --include='*.cs' 2>/dev/null | sed -E 's/NavigateTo\("([^"]*)"/\1/'
  } | sed -E 's/[?#].*$//' | sed -E 's/\{([A-Za-z]+)(:[a-z]+)?\}/{\1}/g' \
    | grep -vE '\.[A-Za-z0-9]+$' | grep -vE '^$' | sort -u)
P02_LINK_N=$(printf '%s\n' "$P02_LINK_HAM" | grep -c . | tr -d ' ')
P02_OLU_L=$(comm -23 <(printf '%s\n' "$P02_LINK_HAM") <(printf '%s\n' "$P02_ROTA_KUMESI"))
P02_OLU_N=$(printf '%s\n' "$P02_OLU_L" | grep -c . | tr -d ' ')
if [ "${P02_LINK_N:-0}" -lt 15 ]; then
  p02_kir "P-02: OLU_IC_BAGLANTI" "ölçüm BOŞ döndü ($P02_LINK_N bağlantı) — sayaç oyunlanmış"
elif [ "${P02_OLU_N:-99}" -eq 0 ]; then
  p02_ok "P-02: OLU_IC_BAGLANTI (href/NavigateTo ⊆ @page)" "0 ($P02_LINK_N bağlantı / $(printf '%s\n' "$P02_ROTA_KUMESI" | grep -c .) rota)"
else
  p02_kir "P-02: OLU_IC_BAGLANTI (href/NavigateTo ⊆ @page)" "$P02_OLU_N ≠ 0 — $(printf '%s\n' "$P02_OLU_L" | tr '\n' ' ')"
fi

# ── P02-1d) ESKI_URUN_ADI = 0 (PAKET P-D2 · Z3/V17/D-d · demo kapısı) ───────
#   KURUCU KARARI (23.09, birebir): "ürünün adı mihenk olacak". MİHENK = mihenk
#   taşı — kuyumcunun altının ayarını ölçtüğü taş. Firma adı ByGatech A.Ş. DEĞİŞMEZ.
#   ÖLÇÜLMÜŞ DURUM (23.09, orkestratör): eski ürün adı 44 dosyada, "Mihenk" 0
#   dosyada. Cumartesi demosunda müşteri ürünün adını tarayıcı sekmesinde ve
#   açılış ekranında GÖRÜR (DEMO-SENARYOSU § 0 Z3, § 3 V17, § 7 A5).
#   Bu sayaç kaynak ağacında eski adın SIFIR olduğunu ölçer; tek kaynak
#   Kuyumcu.Core/I18n/Sozlukler.Marka.cs (Marka.Ad).
#   ELENENLER ve GEREKÇESİ:
#     · bin/obj → derleme çıktısı, kaynak değil.
#     · yayin/  → yayın anlık görüntüsü, kaynaktan YENİDEN ÜRETİLİR (Z1);
#                 kaynak temizse sonraki yayın da temiz doğar.
#     · denetim.sh'ın kendisi → aranan dizge burada tanımlanır (kendini bulur).
#   BEYAZ LİSTE: yok. Yasal belge şablonlarında geçen FİRMA UNVANI ve yasal
#   metinler bu sayacın konusu değildir (orada ürün adı zaten geçmiyor — ölçüldü).
P02_MARKA_ESKI="Kuyumcu"" ""ERP"        # parça parça: betik kendini saymasın
P02_MARKA_KAYNAK="Kuyumcu.Core/I18n/Sozlukler.Marka.cs"
P02_MARKA_TARANAN=$(find . -type f \
    \( -name '*.cs' -o -name '*.razor' -o -name '*.html' -o -name '*.css' \
       -o -name '*.json' -o -name '*.sh' -o -name '*.ps1' -o -name '*.js' \) \
    -not -path '*/bin/*' -not -path '*/obj/*' -not -path './yayin/*' \
    -not -name 'denetim.sh' 2>/dev/null | wc -l | tr -d ' ')
P02_MARKA_L=$(grep -rl "$P02_MARKA_ESKI" . \
    --include='*.cs' --include='*.razor' --include='*.html' --include='*.css' \
    --include='*.json' --include='*.sh' --include='*.ps1' --include='*.js' \
    2>/dev/null | grep -v -E '/(bin|obj)/' | grep -v '^\./yayin/' | grep -v 'denetim\.sh$' | sort -u)
# Demo yolundaki iki AÇILIŞ betiği kaynak ağacın dışında ama kullanıcı onları
# çift tıklar ve konsol başlığında ürün adını GÖRÜR (DEMO-SENARYOSU ADIM 1).
# Ad orada ASCII'dir: .bat konsol kod sayfası (cp857/850) "İ"yi bozar.
P02_MARKA_EKL=$(grep -l "$P02_MARKA_ESKI" ../start.bat ../stop.bat 2>/dev/null || true)
P02_MARKA_L=$(printf '%s\n%s\n' "$P02_MARKA_L" "$P02_MARKA_EKL" | grep -v '^$' | sort -u)
P02_MARKA_N=$(printf '%s\n' "$P02_MARKA_L" | grep -c . | tr -d ' ')
if [ "${P02_MARKA_TARANAN:-0}" -lt 100 ]; then
  p02_kir "P-02: ESKI_URUN_ADI" "tarama BOŞ döndü ($P02_MARKA_TARANAN dosya) — sayaç oyunlanmış"
elif [ ! -f "$P02_MARKA_KAYNAK" ]; then
  p02_kir "P-02: ESKI_URUN_ADI (marka tek kaynağı)" "$P02_MARKA_KAYNAK YOK — tek kaynak sökülmüş"
elif [ "${P02_MARKA_N:-99}" -eq 0 ]; then
  p02_ok "P-02: ESKI_URUN_ADI (marka tek kaynak · P-D2)" "0 ($P02_MARKA_TARANAN dosya tarandı)"
else
  p02_kir "P-02: ESKI_URUN_ADI (marka tek kaynak · P-D2)" "$P02_MARKA_N ≠ 0 — $(printf '%s\n' "$P02_MARKA_L" | tr '\n' ' ')"
fi

# ── P02-1c) demo-tohumla.js prova rotaları TEK KAYNAKTAN doğrulanıyor mu? ───
#   D-c: adım 8'in etiketi '/vade-takibi' yazıyordu, gerçeği '/raporlar/vade-takibi'.
#   'yol' (API ucu) doğru olduğu için prova YEŞİL dönüyor, rapora yanlış rota
#   adı giriyordu. Betik artık rota etiketlerini @page listesine karşı sınıyor.
#   Bu sayaç o kapının betikten SÖKÜLMEDİĞİNİ doğrular (sayacın sayacı).
P02_TOHUM="$P02_KAYIT/betikler/demo-tohumla.js"
if grep -q 'provaRotalariniDogrula' "$P02_TOHUM" 2>/dev/null \
   && grep -q 'uygulamaRotalari' "$P02_TOHUM" 2>/dev/null \
   && [ "$(grep -c 'provaRotalariniDogrula' "$P02_TOHUM" 2>/dev/null)" -ge 2 ]; then
  p02_ok "P-02: demo provası rota tek kaynak kapısı (D-c)" "fail-closed"
else
  p02_kir "P-02: demo provası rota tek kaynak kapısı (D-c)" "demo-tohumla.js'te @page doğrulaması YOK"
fi

# ── P02-1e) DEMO_SENARYO_VERI = 0 (PAKET P-D3 · Z4/D-e · demo kapısı) ───────
#   DEMO-SENARYOSU § 3 (V1–V17) demo kiracısının içeriğini BAĞLAYICI sayar:
#   ≥ 6 cari, ≥ 12 ürün, her üründe ≥ 3 stok, ≥ 8 satış, 2 açık veresiye,
#   1 emanet + bakiyesi sıfır 1 müşteri, POZİTİF kasa (≥ 50.000), 7–10 vitrin
#   kalemi, seçilmiş görünüm, "QA/TEST/DUZ" artığı yok. Betik planı yazmadan
#   ÖNCE sınar (senaryoPlaniniDogrula) ve tohumdan SONRA sunucudan geri okuyup
#   değerlendirir (senaryoOlcumunuDegerlendir; PANEL_NET dâhil — ölçülmüş arıza:
#   stok ALIŞ olarak yazılınca Panel "Net kasa" −1.078.595 TL KIRMIZI çizecekti).
#   Bu sayaç (a) iki kapının betikte DURDUĞUNU, (b) gerçek planın node ile
#   koşulduğunda 0 eksik verdiğini ölçer. node yoksa ÖLÇÜLEMEDİ = KIRMIZI.
#   ÖLÇÜM (TAZE, 2026-09-23): node demo-tohumla.js --plan-dogrula → "eksik": [] · çıkış 0.
P02_SEN_N=0; P02_SEN_AD=""
for P02_SEN_K in 'senaryoPlaniniDogrula(SENARYO)' 'senaryoOlcumunuDegerlendir(olcum' 'PANEL_NET' 'SENARYO KARŞILANMADI' 'acilisStogu'; do
  grep -qF "$P02_SEN_K" "$P02_TOHUM" 2>/dev/null || { P02_SEN_N=$((P02_SEN_N+1)); P02_SEN_AD="$P02_SEN_AD $P02_SEN_K-YOK"; }
done
if command -v node >/dev/null 2>&1; then
  node "$P02_TOHUM" --plan-dogrula >/dev/null 2>&1 || { P02_SEN_N=$((P02_SEN_N+1)); P02_SEN_AD="$P02_SEN_AD plan-dogrula-KIRMIZI"; }
else
  P02_SEN_N=$((P02_SEN_N+1)); P02_SEN_AD="$P02_SEN_AD node-YOK(OLCULEMEDI)"
fi
if [ "$P02_SEN_N" = 0 ]; then
  p02_ok "P-02: DEMO_SENARYO_VERI (P-D3)" "0 (plan § 3'ü karşılıyor · geri okuma fail-closed)"
else
  p02_kir "P-02: DEMO_SENARYO_VERI (P-D3, beklenen 0)" "$P02_SEN_N —$P02_SEN_AD"
fi

# ── P02-2) QA turu 42/42 rota planı fail-closed mı? · B-1 ────────────────────
#   QA turu 6 rotaların 39'unu taradı, 3'ünü SESSİZCE atladı (hepsi parametreli:
#   /gider-pusulalari/{Id}/belge · /siparisler/{Id} · /banka/ekstre/{IthalatId}).
#   Sebep: grup listesi elle yazılıyordu. qa-turu.js artık rota listesini
#   @page'lerden ÜRETİR ve 42 değilse TUR BAŞLAMAZ. Bu sayaç o kapının
#   betikten sökülmediğini doğrular.
#   ÖLÇÜM: grep -c 'ROTA_KAPSAM_BEKLENEN' betikler/qa-turu.js  (≥1 olmalı)
if grep -q 'ROTA_KAPSAM_BEKLENEN' "$P02_QA" 2>/dev/null && grep -q 'rota kapsamı' "$P02_QA" 2>/dev/null; then
  p02_ok "P-02: QA turu rota kapsamı kapısı (B-1)" "fail-closed"
else
  p02_kir "P-02: QA turu rota kapsamı kapısı (B-1)" "qa-turu.js'te 42/42 kapısı YOK"
fi

# ── P02-3) QA son tur kapsamı (cırcır: yalnız artar) · B-1 ───────────────────
#   TABAN 39 = tur 6'da gerçekten taranan rota sayısı (ÖLÇÜLDÜ, kopyalanmadı).
#   Hedef 42; hedefe varana dek UYARI basar, ama DÜŞERSE denetim kırılır.
#   ÖLÇÜM: node -e '...Object.keys(qa-turu-<son>.json .rotalar).length'
P02_QA_TABAN=39
P02_SONTUR=$(ls "$P02_KAYIT"/ham/qa-turu-*.json 2>/dev/null | sed -E 's/.*qa-turu-([0-9]+)\.json/\1/' | sort -n | tail -1)
if [ -n "$P02_SONTUR" ]; then
  P02_QA_ROTA=$(node -e 'const j=require(process.argv[1]);console.log(Object.keys(j.rotalar||{}).length)' "$(cd "$P02_KAYIT/ham" && pwd)/qa-turu-$P02_SONTUR.json" 2>/dev/null || true)
  if [ "$P02_QA_ROTA" -ge "$P02_ROTA_BEKLENEN" ]; then
    p02_ok "P-02: QA son tur rota kapsamı (tur $P02_SONTUR)" "$P02_QA_ROTA/$P02_ROTA_BEKLENEN"
  elif [ "$P02_QA_ROTA" -ge "$P02_QA_TABAN" ]; then
    p02_uyar "P-02: QA son tur rota kapsamı (tur $P02_SONTUR)" "$P02_QA_ROTA/$P02_ROTA_BEKLENEN — taban $P02_QA_TABAN, sonraki tur 42 tarayacak"
  else
    p02_kir "P-02: QA son tur rota kapsamı (tur $P02_SONTUR)" "$P02_QA_ROTA (taban $P02_QA_TABAN'ın ALTINA düştü)"
  fi
else
  p02_kir "P-02: QA son tur rota kapsamı" "ham/qa-turu-N.json yok"
fi

# ── P02-4) MUTORJ_ARTIK (cırcır: yalnız azalır) · R18 ────────────────────────
#   TABAN 16 = bugün ölçülen artık sayısı (5 Web/Pages + 11 Api/Modules).
#   SİLME KURUCU KALEMİDİR (K-4): TurkReport hook'u 'rm'/'mv' engelliyor.
#   Bu yüzden sayaç ">0 = FAIL" değil, "artarsa FAIL"dir; mut.sh her koşumda
#   yeni artık bırakır, sayacın artması kimsenin temizlemediğini gösterir.
#   ÖLÇÜM: find Kuyumcu.Web Kuyumcu.Api -name '*.mutorj*' | wc -l   → 0 (28.09 16:25 kurucu 16 artığı sildi)
P02_MUTORJ_TAVAN=0
P02_MUTORJ=$(find "$WEB" "$API" -name '*.mutorj*' 2>/dev/null | wc -l | tr -d ' ')
if [ "$P02_MUTORJ" -le "$P02_MUTORJ_TAVAN" ]; then
  p02_ok "P-02: MUTORJ_ARTIK (tavan $P02_MUTORJ_TAVAN, K-4)" "$P02_MUTORJ"
else
  p02_kir "P-02: MUTORJ_ARTIK (tavan $P02_MUTORJ_TAVAN, K-4)" "$P02_MUTORJ — yeni artık bırakılmış"
fi

# ── P02-4b) ARTIK_YEDEK (cırcır: yalnız azalır) · P-40 ──────────────────────
#   MUTORJ_ARTIK yalnız Web+Api altındaki *.mutorj* dosyalarını sayar; mutasyon
#   turlarından kalan *.bak / *.mutbak artıkları denetimin KÖR NOKTASIYDI
#   (P-40 hasım doğrulaması, DUSUK bulgu). TABAN 4 = bugün TAZE ölçülen:
#   Kuyumcu.Tests/{AramaHttpTestleri,MadenDefteriTestleri,P21CiroRucuTestleri}.cs.bak
#   ve denetim.sh.mutbak. Silme KURUCU kalemidir (hook rm/mv engelliyor), bu
#   yüzden ">0 = FAIL" değil "artarsa FAIL". bin/obj hariç tutulur.
P02_YEDEK_TAVAN=4
P02_YEDEK=$(find . \( -name '*.bak' -o -name '*.mutbak' \) -not -path '*/bin/*' -not -path '*/obj/*' 2>/dev/null | wc -l | tr -d ' ')
if [ "$P02_YEDEK" -le "$P02_YEDEK_TAVAN" ]; then
  p02_ok "P-02: ARTIK_YEDEK (tavan $P02_YEDEK_TAVAN, K-4)" "$P02_YEDEK"
else
  p02_kir "P-02: ARTIK_YEDEK (tavan $P02_YEDEK_TAVAN, K-4)" "$P02_YEDEK — yeni .bak/.mutbak artigi"
fi

# ── P02-5) GREP_INCLUDE_EKSIK = 0 · R18 ─────────────────────────────────────
#   Bu betikteki hiçbir sayaç .razor dizinlerini include süzgeci OLMADAN
#   taramaz; yoksa .mutorj artıkları sayacı kirletir ve taban sahte çıkar.
#   ÖLÇÜM (yalnız DİZİN taramaları; tek dosya grepleri için include anlamsızdır):
#     grep -nE "grep[^|]*\$WEB/(Pages|Bilesenler|Layout)\"" denetim.sh \
#            | grep -v "include='\*.razor'" | grep -vE '^[0-9]+:#' | wc -l  → 0
P02_INC=$(grep -nE 'grep[^|]*\$WEB/(Pages|Bilesenler|Layout)"' "$BETIK_YOLU" 2>/dev/null \
  | grep -v "include='\*.razor'" | grep -vE '^[0-9]+:[[:space:]]*#' | wc -l | tr -d ' ')
# Payda yoklaması (P-30b): betikte dizin taraması HİÇ bulunamadıysa "0 ihlal"
# anlamsızdır — okuma boş dönmüştür; bu "temiz" değil "ölçülemedi"dir.
P02_INC_PAYDA=$(grep -nE 'grep[^|]*\$WEB/(Pages|Bilesenler|Layout)"' "$BETIK_YOLU" 2>/dev/null \
  | grep -vE '^[0-9]+:[[:space:]]*#' | wc -l | tr -d ' ')
if [ "${P02_INC_PAYDA:-0}" = "0" ]; then
  p02_kir "P-02: GREP_INCLUDE_EKSIK" "ÖLÇÜLEMEDİ(betik kendi yolundan okunamadı — payda 0)"
elif [ "$P02_INC" = "0" ]; then
  p02_ok "P-02: GREP_INCLUDE_EKSIK" "0"
else
  p02_kir "P-02: GREP_INCLUDE_EKSIK" "$P02_INC (include'suz grep .mutorj sayar)"
  grep -nE 'grep[^|]*\$WEB/(Pages|Bilesenler|Layout)"' "$BETIK_YOLU" | grep -v "include='\*.razor'" | grep -vE '^[0-9]+:[[:space:]]*#' | cut -c1-110 | sed 's/^/      → /'
fi

# ── P02-6) TEK_EL_IHLAL kapısı kapi.sh'ta duruyor mu? · BK-05 ───────────────
#   Gerçek kilit mantığı kapi.sh içindedir (orkestratör kapısı); burada yalnız
#   "sökülmedi" denetlenir. kapi.sh bir kez bilerek kırılıp FAIL verdi (rapor § Mutasyon).
#   ÖLÇÜM: grep -c 'TEK_EL_IHLAL' betikler/kapi.sh  (≥2: sayaç + kilit dosyası)
P02_TEKEL=$(grep -c 'TEK_EL_IHLAL' "$P02_KAPI" 2>/dev/null || true)
if [ "$P02_TEKEL" -ge 2 ]; then
  p02_ok "P-02: TEK_EL_IHLAL kapısı (kapi.sh)" "kurulu"
else
  p02_kir "P-02: TEK_EL_IHLAL kapısı (kapi.sh)" "kapi.sh'ta sayaç yok"
fi

# ── P02-7) B-53: mut.sh sürümlendi mi? ──────────────────────────────────────
#   Mutasyon kanıtı zinciri oturum geçici dizininde yaşayamaz; oturum kapanınca
#   yöntem kayboluyordu. Betik artık depoda ve çalıştırılabilir olmalı.
#   ÖLÇÜM: test -x dotnet/mut.sh
if [ -x "mut.sh" ] && grep -q 'cmp -s' mut.sh 2>/dev/null; then
  p02_ok "P-02: mut.sh sürümlü + cmp kanıtı (B-53)" "var"
else
  p02_kir "P-02: mut.sh sürümlü + cmp kanıtı (B-53)" "dotnet/mut.sh yok / çalıştırılabilir değil / cmp kanıtı yok"
fi

# ── P02-8) OLCUM_TABANI_KOPYA = 0 · BK-07 (fail-closed) ─────────────────────
#   Tabanlar ŞİMDİ yeniden ölçülür ve ham/olcum-2026-09-18.json ile karşılaştırılır.
#   Uyuşmazlık iki şeyden biridir: (a) depo değişti, taban güncellenmedi;
#   (b) taban belgeden kopyalandı. İkisi de kapıyı kırar.
#   ÖLÇÜM: her sayaç kendi yorumundaki komutla yeniden ölçülür (aşağıda).
if [ -s "$P02_OLCUM" ]; then
  P02_REC_OLC=$(node -e 'const j=require(process.argv[1]);console.log(j.sayaclar.SAYFA_RECORD_TAVAN.deger)' "$(cd "$P02_KAYIT/ham" && pwd)/olcum-2026-09-18.json" 2>/dev/null || echo -1)
  P02_VAR_OLC=$(node -e 'const j=require(process.argv[1]);console.log(j.sayaclar.BEKLENEN_VARLIK.deger)' "$(cd "$P02_KAYIT/ham" && pwd)/olcum-2026-09-18.json" 2>/dev/null || echo -1)
  P02_ROT_OLC=$(node -e 'const j=require(process.argv[1]);console.log(j.sayaclar.ROTA_KAPSAM.deger)' "$(cd "$P02_KAYIT/ham" && pwd)/olcum-2026-09-18.json" 2>/dev/null || echo -1)
  P02_SAPMA=0
  [ "$P02_VAR_OLC" = "$TOPLAM" ]      || P02_SAPMA=$(( P02_SAPMA + 1 ))
  [ "$P02_ROT_OLC" = "$P02_ROTA" ]    || P02_SAPMA=$(( P02_SAPMA + 1 ))
  [ "$P02_REC_OLC" = "$SAYFA_RECORD" ] || P02_SAPMA=$(( P02_SAPMA + 1 ))
  if [ "$P02_SAPMA" = "0" ]; then
    p02_ok "P-02: OLCUM_TABANI_KOPYA" "0 (varlık $TOPLAM · rota $P02_ROTA · record $SAYFA_RECORD)"
  else
    p02_kir "P-02: OLCUM_TABANI_KOPYA" "$P02_SAPMA sapma — olcum-2026-09-18.json ile canlı ölçüm ayrıştı"
    printf "      → canlı: varlık=%s rota=%s record=%s · dosya: varlık=%s rota=%s record=%s\n" \
      "$TOPLAM" "$P02_ROTA" "$SAYFA_RECORD" "$P02_VAR_OLC" "$P02_ROT_OLC" "$P02_REC_OLC"
  fi
else
  p02_kir "P-02: OLCUM_TABANI_KOPYA" "ham/olcum-2026-09-18.json yok"
fi

# ── P02-9) YAYIN_TAZE (B-2) — UYARI MODU, sebebi yazılı ─────────────────────
#   Yayın damgası = ağaç damgası olmalı: 5199'daki anlık görüntü kaynaktan
#   eskiyse kurucu OLMAYAN bir ürünü inceler. Kapı fail-closed olmalıdır AMA
#   K-20 (iCloud eşitlemesi) mtime'ları oynattığı sürece sahte "bayat" üretir
#   (PLAN § 2 P-02 riski). K-20 kapanana dek UYARI; kapandığında bu blok
#   p02_uyar → p02_kir yapılır (tek satır).
#   ÖLÇÜM: en yeni kaynak mtime > yayın dll mtime ise bayat.
P02_YAYIN="/private/tmp/claude-501/-Users-ramazanakkocan-Desktop-ByGatech-projeler-jewellery/f8597a15-bda0-4765-a57f-77f2b6380580/scratchpad/qa-yayin/Kuyumcu.Api.dll"
if [ -f "$P02_YAYIN" ]; then
  P02_YENI=$(find "$API" "$WEB" ../dotnet/Kuyumcu.Core -name '*.cs' -o -name '*.razor' 2>/dev/null \
    | grep -v '/obj/\|/bin/' | xargs stat -f '%m' 2>/dev/null | sort -n | tail -1)
  P02_DLL=$(stat -f '%m' "$P02_YAYIN" 2>/dev/null || true)
  if [ "${P02_YENI:-0}" -le "${P02_DLL:-0}" ]; then
    p02_ok "P-02: YAYIN_TAZE (5199 anlık görüntüsü)" "taze"
  else
    p02_uyar "P-02: YAYIN_TAZE (5199 anlık görüntüsü)" "BAYAT — kaynak yayından yeni; yayinla.sh koşulmalı (K-20 açıkken sahte olabilir)"
  fi
else
  p02_uyar "P-02: YAYIN_TAZE (5199 anlık görüntüsü)" "yayın anlık görüntüsü yok (qa-yayin)"
fi

# ── P02-10) Yayın ortamı ÜRETİM mi? · B-2 ───────────────────────────────────
#   İnceleme sunucusu Development'ta koşarsa Swagger açık, HSTS/HTTPS kapalı,
#   bekleyen migration kapısı devre dışıdır — kurucu ÜRÜNÜ değil, geliştirme
#   kısayollarını inceler. qa-sunucu.sh Production'a sabitlendi ve launch.json
#   artık SÜRÜMLÜ betiği gösterir (eskiden scratchpad kopyasını gösteriyordu:
#   oturum kapanınca 5199 açılmıyordu — B-2'nin kökü).
#   ÖLÇÜM: grep -c 'ASPNETCORE_ENVIRONMENT=Production' betikler/qa-sunucu.sh
#          grep -c 'betikler/qa-sunucu.sh' .claude/launch.json
P02_ORT=0
grep -q 'ASPNETCORE_ENVIRONMENT=Production' "$P02_SUNUCU" 2>/dev/null || P02_ORT=$(( P02_ORT + 1 ))
grep -q 'dokumanlar/09-uygulama-kayitlari/betikler/qa-sunucu.sh' "$P02_LAUNCH" 2>/dev/null || P02_ORT=$(( P02_ORT + 1 ))
if [ "$P02_ORT" = "0" ]; then
  p02_ok "P-02: yayın ortamı ÜRETİM + sürümlü betik (B-2)" "kurulu"
else
  p02_kir "P-02: yayın ortamı ÜRETİM + sürümlü betik (B-2)" "$P02_ORT eksik"
fi

# ── P02-11) ICLOUD_ESITLEME_ACIK · K-20 / R15 / BK-42 ───────────────────────
#   Açıkken TÜM SÜRE ölçümleri "geçersiz taban" damgası alır (tek derleme 30 dk
#   ölçülmüştü). Sayı ölçümleri etkilenmez. Kapı UYARI verir, iş durmaz.
#   ÖLÇÜM: test -d ~/Library/Mobile\ Documents/com~apple~CloudDocs/Desktop && pgrep -x bird
if [ -d "$HOME/Library/Mobile Documents/com~apple~CloudDocs/Desktop" ] && pgrep -x bird >/dev/null 2>&1; then
  p02_uyar "P-02: ICLOUD_ESITLEME_ACIK (K-20)" "AÇIK — bu dalganın SÜRE ölçümleri GEÇERSİZ TABAN damgalıdır"
else
  p02_ok "P-02: ICLOUD_ESITLEME_ACIK (K-20)" "kapalı — süre ölçümleri geçerli"
fi

# ── P02-12) SIR_KAYNAK_OTURUMLUK = 0 · B-2'nin KÖKÜ (fail-closed) ───────────
#   ÖNCEKİ KUSUR: qa-sunucu.sh jwt/bağlantı/şifreleme anahtarını YALNIZ oturuma
#   özgü scratchpad yolundan okuyordu. O dizin oturumla birlikte yok olur →
#   betik exit 3/5/8 ile ölür ve kurucunun 5199'u BİR DAHA AÇILMAZ. Kalıcı bir
#   sır dizini (KUYUMCU_SIR_DIZINI ya da ~/.kuyumcu) BİRİNCİL kaynak olmalıdır;
#   scratchpad yalnız geriye dönük YEDEK olabilir.
#   ÖLÇÜM: grep -c 'SIR_KALICI' betikler/qa-sunucu.sh   (≥3 beklenir)
#          grep -c 'SIR_GECICI' betikler/qa-sunucu.sh   (≥1 beklenir, yalnız yedek)
#          sır okuyan satırlarda doğrudan scratchpad yolu = 0 olmalı
# ⚠ `grep -c` eşleşme yokken "0" BASAR ve 1 ile ÇIKAR; `|| echo 0` eklenirse
#   değer "0\n0" olur ve `-eq` karşılaştırması patlar. Bu yüzden `head -1`.
P02_SIR_KALICI=$(grep -c 'SIR_KALICI' "$P02_SUNUCU" 2>/dev/null | head -1)
P02_SIR_DOGRUDAN=$(grep -cE '^(GIZLI_JWT|GIZLI_BAGLANTI|GIZLI)=/private/tmp' "$P02_SUNUCU" 2>/dev/null | head -1)
if [ "$P02_SIR_KALICI" -ge 3 ] && [ "$P02_SIR_DOGRUDAN" -eq 0 ]; then
  p02_ok "P-02: SIR_KAYNAK_OTURUMLUK (qa-sunucu.sh)" "0 — kalıcı dizin birincil"
else
  p02_kir "P-02: SIR_KAYNAK_OTURUMLUK (qa-sunucu.sh)" \
    "kalıcı=$P02_SIR_KALICI doğrudan-scratchpad=$P02_SIR_DOGRUDAN (sır kaynağı oturumluk → 5199 geri gelmez)"
fi

# ── P02-13) DAMGASIZ_YAYIN_KAPISI · B-2 (fail-closed) ──────────────────────
#   DoD: "uyuşmazlıkta 5199 açılmaz". Damgasızlık da bir uyuşmazlıktır — ama
#   P-02 ÖNCESİ yayınlarda damga yoktu, hepsini reddetmek 5199'u sebepsiz
#   kapatırdı. Çözüm: yayinla.sh ilk başarılı koşumunda ham/YAYIN-DAMGA-ZORUNLU
#   anahtarını yazar; o andan sonra damgasız anlık görüntü exit 9 ile reddedilir.
#   ÖLÇÜM: grep -c 'YAYIN-DAMGA-ZORUNLU' betikler/qa-sunucu.sh betikler/yayinla.sh
#          grep -c 'exit 9' betikler/qa-sunucu.sh
P02_YAYINLA="$P02_KAYIT/betikler/yayinla.sh"
P02_DZ=0
grep -q 'YAYIN-DAMGA-ZORUNLU' "$P02_SUNUCU" 2>/dev/null || P02_DZ=$(( P02_DZ + 1 ))
grep -q 'YAYIN-DAMGA-ZORUNLU' "$P02_YAYINLA" 2>/dev/null || P02_DZ=$(( P02_DZ + 1 ))
grep -q 'exit 9' "$P02_SUNUCU" 2>/dev/null || P02_DZ=$(( P02_DZ + 1 ))
if [ "$P02_DZ" = "0" ]; then
  p02_ok "P-02: DAMGASIZ_YAYIN_KAPISI (B-2)" "kurulu (exit 9)"
else
  p02_kir "P-02: DAMGASIZ_YAYIN_KAPISI (B-2)" "$P02_DZ eksik — damgasız yayın hâlâ servis edilebilir"
fi

# ── P02-14) QA_TUR_SURESI_OLCUM · BK-39 / Mustafa K-F2 (DEVİRLİ FAIL-CLOSED) ─
#   "≈8 ajan-saat/tur" hâlâ VARSAYIM ise tüm dalga bütçeleri varsayıma dayanır.
#   Ölçüm P-02'de KOŞULAMADI (gerekçe: qa-tur-suresi.json § devir_karari — tur
#   çok-ajanlı tarayıcı iş akışıdır, 5199'a bağlıdır, paket bütçesinin katıdır)
#   ve Tamer Bey mührüyle Dalga 1 kapısındaki QA turuna DEVREDİLDİ.
#   ⚠ DEVİR SÜRESİZ DEĞİLDİR (hasım doğrulayıcı koşulu, 2026-09-18): devir
#   penceresi kapanınca bu satır UYARI olmaktan çıkıp DENETİMİ KIRAR. Pencere
#   iki koşuldan biriyle kapanır:
#     (a) devrin hedefi olan tur KOŞTU — ham/qa-turu-N.json'da N > devir turu
#         var ama dosyaya hâlâ ölçüm yazılmamış (devir sözü tutulmadı), ya da
#         → burada "devir turu" = devir kararı anındaki son tur (json'da yazılı)
#     (b) son tarih geçti — json'daki devir_son_tarih (YYYY-MM-DD).
#   ÖLÇÜM:
#     node -e 'console.log(require("…/ham/qa-tur-suresi.json").gecerli_ortalama_saat)'
#     node -e 'console.log(require("…/ham/qa-tur-suresi.json").devir_son_tarih)'
#     ls ham/qa-turu-*.json | sed -E 's/.*-([0-9]+)\.json/\1/' | sort -n | tail -1
P02_SURE_DOSYA="$P02_KAYIT/ham/qa-tur-suresi.json"
P02_SURE_HAM="$(cd "$P02_KAYIT/ham" && pwd)/qa-tur-suresi.json"
P02_SURE=$(node -e 'const j=require(process.argv[1]);console.log(j.gecerli_ortalama_saat===null||j.gecerli_ortalama_saat===undefined?"YOK":j.gecerli_ortalama_saat)' "$P02_SURE_HAM" 2>/dev/null || echo YOK)
P02_SURE_SONTARIH=$(node -e 'const j=require(process.argv[1]);console.log(j.devir_son_tarih||"")' "$P02_SURE_HAM" 2>/dev/null || echo "")
P02_SURE_DEVIRTUR=$(node -e 'const j=require(process.argv[1]);console.log(j.devir_turu===undefined?-1:j.devir_turu)' "$P02_SURE_HAM" 2>/dev/null || echo -1)
P02_BUGUN=$(date +%Y-%m-%d)
# ── ÜÇ DURUM AYRILDI (2026-09-21, orkestratör) ───────────────────────────────
#   Eski kural iki durumu aynı sayıyordu: "hiç ölçüm yok" ile "ölçüm var ama
#   K-20 (iCloud) açık olduğu için geçersiz damgalı". Tur 7 koşup 1,46 saat
#   ÖLÇÜLDÜĞÜ hâlde kapı "ölçüm YAZILMADI" diyerek kırdı ve tüm mühendislik işi
#   kurucunun tek bir iCloud tıkına bağlı kaldı. Oysa K-20 açıkken alınan ölçüm
#   bir ÜST SINIRDIR: eşitleme her şeyi yavaşlatır, temiz ortamdaki süre bundan
#   KÜÇÜK olur — bütçeyi üst sınırla kurmak karamsar ve güvenlidir.
#   Artık: (1) temiz ölçüm → ✓ · (2) üst sınır ölçümü + son tarih geçmemiş → ⚠
#   · (3) üst sınır ölçümü ama son tarih GEÇTİ → ✗ · (4) hiç ölçüm yok → ✗.
#   SON TARİH GEVŞETİLMEDİ (2026-09-30) ve "hiç ölçüm yok" hâlâ kapıyı kırar.
P02_UST_SINIR=$(node -e 'const j=require(process.argv[1]);console.log(j.ust_sinir_saat===null||j.ust_sinir_saat===undefined?"YOK":j.ust_sinir_saat)' "$P02_SURE_HAM" 2>/dev/null || echo YOK)
P02_UST_SONTARIH=$(node -e 'const j=require(process.argv[1]);console.log(j.ust_sinir_son_tarih||"")' "$P02_SURE_HAM" 2>/dev/null || echo "")
if [ "$P02_SURE" != "YOK" ]; then
  p02_ok "P-02: QA_TUR_SURESI_OLCUM (BK-39)" "$P02_SURE ajan-saat (temiz taban, ölçüldü)"
elif [ "$P02_UST_SINIR" != "YOK" ] && [ -n "$P02_UST_SONTARIH" ] && [ "$P02_BUGUN" \> "$P02_UST_SONTARIH" ]; then
  p02_kir "P-02: QA_TUR_SURESI_OLCUM (BK-39)" \
    "üst sınır $P02_UST_SINIR sa var ama TEMİZ TABAN yok ve son tarih $P02_UST_SONTARIH geçti — K-20 kapatılmalı"
elif [ "$P02_UST_SINIR" != "YOK" ]; then
  p02_uyar "P-02: QA_TUR_SURESI_OLCUM (BK-39)" \
    "ÜST SINIR $P02_UST_SINIR ajan-saat (K-20 açık — temiz taban yok; $P02_UST_SONTARIH'e dek geçerli, sonra KIRAR)"
elif [ -z "$P02_SURE_SONTARIH" ] || [ "$P02_SURE_DEVIRTUR" = "-1" ]; then
  # Devir kaydı eksik: süresiz "sonra ölçeriz" YASAK (Anayasa md.1).
  p02_kir "P-02: QA_TUR_SURESI_OLCUM (BK-39)" "ölçüm YOK ve devir kaydı eksik (devir_son_tarih / devir_turu)"
elif [ -n "$P02_SONTUR" ] && [ "$P02_SONTUR" -gt "$P02_SURE_DEVIRTUR" ]; then
  p02_kir "P-02: QA_TUR_SURESI_OLCUM (BK-39)" \
    "devir turu $P02_SURE_DEVIRTUR sonrası tur $P02_SONTUR KOŞTU ama süre ölçümü YAZILMADI — devir penceresi kapandı"
elif [ "$P02_BUGUN" \> "$P02_SURE_SONTARIH" ]; then
  p02_kir "P-02: QA_TUR_SURESI_OLCUM (BK-39)" \
    "devir son tarihi $P02_SURE_SONTARIH geçti ($P02_BUGUN), ölçüm hâlâ YOK — 8 ajan-saat varsayımı resmen çürük"
else
  p02_uyar "P-02: QA_TUR_SURESI_OLCUM (BK-39)" \
    "ÖLÇÜM YOK — devir açık (tur >$P02_SURE_DEVIRTUR ya da $P02_SURE_SONTARIH'e dek; sonrasında DENETİM KIRILIR)"
fi

# ── P02-15) URETIM_KUNYE_KANITI · B-2 DoD ×  K-5 / BK-14 ───────────────────
#   P-02 DoD'u künyede "Ortam: Üretim" CANLI kanıtı ister. Bu kanıt bugün
#   ALINAMAZ ve sebebi bir eksiklik değil, İSTENEN DAVRANIŞTIR: üretim açılış
#   kapısı (UretimAcilisDenetimi) geliştirme MySQL parolasını parmak iziyle
#   tanır ve süreci açtırmaz. BK-14 zaten "K-5 tamamlanmadan P-02 Production'a
#   açılmaz" der. Yani kanıtın yokluğu K-5'in (kurucu kalemi) açık olmasıdır.
#   ⚠ TUZAK: K-5 kapanınca bu borç SESSİZCE kaybolur — parola artık kara
#   listede olmadığı için kimse künye kanıtını istemez. Bu yüzden sayaç ters
#   çalışır: K-5 AÇIKKEN uyarı, K-5 KAPANDIĞINDA kanıt dosyası yoksa KIRAR.
#   ⚠ Parola HİÇBİR koşulda ekrana basılmaz; yalnız SHA-256 parmak izi
#     UretimAcilisDenetimi.cs'teki listeyle karşılaştırılır (değer geri üretilemez).
#   ÖLÇÜM (parola doğru ayrıştırma kalıbı — `grep MYSQL_ROOT_PASSWORD | head -1`
#     YORUM SATIRINI yakalar ve 3 karakterlik sahte jeton üretir, KULLANMAYIN):
#     P=$(awk -F': ' '/^[[:space:]]*MYSQL_ROOT_PASSWORD:/ {print $2; exit}' docker-compose.yml)
#     printf '%s' "$P" | shasum -a 256 | cut -d' ' -f1 | xxd -r -p | base64
#     grep -c "<parmak izi>" Kuyumcu.Api/Common/UretimAcilisDenetimi.cs
#     test -s dokumanlar/09-uygulama-kayitlari/ham/uretim-kunye-kaniti.json
P02_KUNYE="$P02_KAYIT/ham/uretim-kunye-kaniti.json"
P02_SIRDENETIM="Kuyumcu.Api/Common/UretimAcilisDenetimi.cs"
#   ⚠ K-5 İKİ SIRDIR: [0] geliştirme Jwt:Secret, [1] geliştirme MySQL parolası.
#     CANLI ÖLÇÜLDÜ (2026-09-18, 5198 Production denemesi): açılışı İLK durduran
#     Jwt:Secret'tir ("Jwt:Secret varsayılan/örnek değerde … fail-closed").
#     Yalnız parolayı ölçen bir sayaç, parola rotasyonundan sonra "K-5 kapandı"
#     sanıp HÂLÂ ALINAMAYAN bir kanıtı isterdi. İKİSİ de temiz olmadan K-5 açıktır.
P02_K5_ACIK=0
P02_PAROLA=$(awk -F': ' '/^[[:space:]]*MYSQL_ROOT_PASSWORD:/ {print $2; exit}' ../docker-compose.yml 2>/dev/null)
if [ -n "$P02_PAROLA" ]; then
  P02_PI=$(printf '%s' "$P02_PAROLA" | shasum -a 256 2>/dev/null | cut -d' ' -f1 | xxd -r -p 2>/dev/null | base64)
  unset P02_PAROLA
  if [ -n "$P02_PI" ] && grep -qF "$P02_PI" "$P02_SIRDENETIM" 2>/dev/null; then P02_K5_ACIK=1; fi
  unset P02_PI
fi
#   ÖLÇÜM (Jwt): node -e 'JSON.parse(fs...appsettings.Development.json).Jwt.Secret' → SHA-256 → base64
P02_JWT=$(node -e 'const j=require(process.argv[1]);process.stdout.write((j.Jwt&&j.Jwt.Secret)||"")' "$(cd Kuyumcu.Api && pwd)/appsettings.Development.json" 2>/dev/null)
if [ -n "$P02_JWT" ]; then
  P02_PI=$(printf '%s' "$P02_JWT" | shasum -a 256 2>/dev/null | cut -d' ' -f1 | xxd -r -p 2>/dev/null | base64)
  case "$P02_JWT" in DEGISTIR*) P02_K5_ACIK=1 ;; esac
  unset P02_JWT
  if [ -n "$P02_PI" ] && grep -qF "$P02_PI" "$P02_SIRDENETIM" 2>/dev/null; then P02_K5_ACIK=1; fi
  unset P02_PI
fi
if [ -s "$P02_KUNYE" ]; then
  p02_ok "P-02: URETIM_KUNYE_KANITI (B-2 DoD)" "canlı kanıt dosyası var"
elif [ "$P02_K5_ACIK" = "1" ]; then
  p02_uyar "P-02: URETIM_KUNYE_KANITI (B-2 DoD)" \
    "ALINAMAZ — K-5 AÇIK (geliştirme sırrı kara listede: Jwt ve/veya MySQL); BK-14 gereği Production açılmaz. P-02 bu kalemde KISMEN."
else
  p02_kir "P-02: URETIM_KUNYE_KANITI (B-2 DoD)" \
    "K-5 KAPANDI ama künye canlı kanıtı (ham/uretim-kunye-kaniti.json) YOK — tek açılışla alınmalı"
fi
# <<< P-02

# >>> P-03 — SÖZLEŞME-1: Kuyumcu.Contracts + tip/ad sözleşmesi
#     (cırcır; PLAN § 2 P-03, kimlik R-10, bağlı koşul BK-01)
#
# NEDEN: istemci tipi ile sunucu DTO'su arasındaki ad/TİP kayması DERLEME HATASI
# VERMEZ; JSON ayrıştırıcısı eşleşmeyen alanı atlar ve ekran sessizce null/0
# gösterir (QA turu 2 dersi). Bu yüzden "aynı modelin iki ayrı tanımı" sayılır.
#
# ÖLÇÜM KOMUTU (BK-07 — taban belgeden kopyalanmadı, burada yeniden koşuyor):
#   sunucu:  grep -rhoE '^[[:space:]]*(public|internal|private|sealed|abstract)[A-Za-z ]*record [A-Za-z0-9_]+' \
#              --include='*.cs' Kuyumcu.Api Kuyumcu.Core | awk '{print $NF}' | sort -u
#   istemci: aynı desen, Kuyumcu.Contracts (*.cs) + Pages/Bilesenler/Layout (*.razor)
#   sayaç:   comm -12 <(sunucu) <(istemci) | wc -l      → 0 olmalı (-eq)
#
# KAPSAM (dürüstlük notu, Anayasa md.1): ölçüm P-03'ün sahiplendiği yüzeydir.
# Kuyumcu.Web/Servisler/ PLAN § 7'ye göre P-07'nin TEK ELİdir; oradaki 6 çift
# tanım (MasakGereksinim, PosCihazSatir, PosTahsilatSatir, PosTaksitOranSatir,
# PosValorSatir, SurumKunyesi) P-07'ye DEVREDİLDİ — gizlenmedi, sayaca dâhil
# edilmedi çünkü bu paket o dosyalara dokunamaz.
P03_DESEN='^[[:space:]]*(public|internal|private|sealed|abstract)[A-Za-z ]*record [A-Za-z0-9_]+'
P03_TMP_S=$(grep -rhoE "$P03_DESEN" --include='*.cs' "$API" Kuyumcu.Core 2>/dev/null | awk '{print $NF}' | sort -u)
P03_TMP_I=$( { grep -rhoE "$P03_DESEN" --include='*.cs' Kuyumcu.Contracts 2>/dev/null; \
               grep -rhoE "$P03_DESEN" --include='*.razor' "$WEB/Pages" "$WEB/Bilesenler" "$WEB/Layout" 2>/dev/null; } \
             | awk '{print $NF}' | sort -u)
CONTRACTS_DISI_MODEL=$(comm -12 <(printf '%s\n' "$P03_TMP_S") <(printf '%s\n' "$P03_TMP_I") | grep -c . | tr -d ' ')
if [ "$CONTRACTS_DISI_MODEL" -eq 0 ]; then
  printf "  ✓ %-46s %s (-eq 0)\n" "P-03: CONTRACTS_DISI_MODEL (ikinci tanım)" "$CONTRACTS_DISI_MODEL"
else
  printf "  ✗ %-46s %s (-eq 0 olmalı — aynı modelin iki tanımı)\n" "P-03: CONTRACTS_DISI_MODEL (ikinci tanım)" "$CONTRACTS_DISI_MODEL"
  printf "      → %s\n" "$(comm -12 <(printf '%s\n' "$P03_TMP_S") <(printf '%s\n' "$P03_TMP_I") | tr '\n' ' ')"
  HATA=1
fi

# Kuyumcu.Contracts projesi gerçekten var ve çözüme bağlı mı? (mekanizma kapısı)
if [ -f "Kuyumcu.Contracts/Kuyumcu.Contracts.csproj" ] && grep -q "Kuyumcu.Contracts" Kuyumcu.sln; then
  P03_TIP=$(grep -rhoE "$P03_DESEN" --include='*.cs' Kuyumcu.Contracts | wc -l | tr -d ' ')
  printf "  ✓ %-46s %s tip\n" "P-03: Kuyumcu.Contracts projesi" "$P03_TIP"
else
  printf "  ✗ %-46s yok / çözüme eklenmemiş\n" "P-03: Kuyumcu.Contracts projesi"; HATA=1
fi

# ── KAPSAM SAYACI: SOZLESME_KILITSIZ_TIP (azalan cırcır) ────────────────────
# NEDEN AYRI SAYAÇ: yukarıdaki CONTRACTS_DISI_MODEL yalnız "aynı adın İKİNCİ
# TANIMI" kapısıdır; taşımadan sonra istemci tipleri sunucu DTO'larından bilerek
# FARKLI ad taşır (Satir ⇄ OnizlemeSatiri), dolayısıyla o kesişim doğal olarak
# 0'dır ve ad/TİP KAYMASI riskini YAKALAYAMAZ (hasım turu ORTA bulgusu, 2026-09-18).
# Asıl kapı budur: razor'da `Api.XAsync<T>` ile ayrıştırılan KAÇ tipin sunucu
# tarafıyla gerçekten kilitli olduğunu ölçer.
#
# KİLİTLİ SAYILMA ÖLÇÜTÜ (Kuyumcu.Tests/SozlesmeKapsamTestleri.cs uygular):
#   (a) sunucu AYNI tipi imzasında kullanıyor (tek tanım → derleyici kilidi), ya da
#   (b) SozlesmeSekilTestleri canlı HTTP yanıtının şeklini tipe karşı doğruluyor, ya da
#   (c) SozlesmeYansimaTestleri.Ciftler() tablosunda yansımayla eşleniyor.
#
# ÖLÇÜM KOMUTU (BK-07 — taban belgeden kopyalanmadı, burada taze koşuyor):
#   toplam:  grep -rhoE "Api\.(Get|Post|Patch|Put|Delete)Async<[A-Za-z0-9_.]+" --include='*.razor' \
#              Kuyumcu.Web/Pages Kuyumcu.Web/Bilesenler Kuyumcu.Web/Layout \
#              | sed -E 's/.*<//;s/.*\.//' | sort -u | grep -vxE 'JsonElement|object|List' | wc -l
#   kilitsiz: Kuyumcu.Tests/SozlesmeKapsamTestleri.cs `KilitsizListesi()` dizisinin uzunluğu
#             (dizinin GERÇEĞİ yansıttığını testin kendisi kanıtlar — liste yalan söyleyemez)
#
# TAVAN YALNIZ AZALIR. Bir tip kilitlendiğinde listeden silinir ve tavan düşürülür.
# 2026-09-18 taze ölçüm: 72 hedefin 59'u kilitsiz (13 kilitli).
# 2026-10-01 KESIF-P6: Panel'deki PatchAsync<KaynakSonuc> kaldırıldı → 58.
SOZLESME_KILITSIZ_TAVAN=58

P03_TEST="Kuyumcu.Tests/SozlesmeKapsamTestleri.cs"
SOZLESME_HEDEF=$(grep -rhoE "Api\.(Get|Post|Patch|Put|Delete)Async<[A-Za-z0-9_.]+" --include='*.razor' \
                   "$WEB/Pages" "$WEB/Bilesenler" "$WEB/Layout" 2>/dev/null \
                 | sed -E 's/.*<//;s/.*\.//' | sort -u | grep -vxE 'JsonElement|object|List' | grep -c .)
SOZLESME_KILITSIZ_TIP=$(awk '/KilitsizListesi\(\) => new\[\]/{f=1;next} f&&/^    };/{f=0} f' "$P03_TEST" \
                        | grep -cE '^[[:space:]]*"[A-Za-z0-9_]+",')
if [ ! -f "$P03_TEST" ] || [ "$SOZLESME_KILITSIZ_TIP" -eq 0 ]; then
  printf "  ✗ %-46s ölçülemedi (cırcır listesi bulunamadı)\n" "P-03: SOZLESME_KILITSIZ_TIP"; HATA=1
elif [ "$SOZLESME_KILITSIZ_TIP" -le "$SOZLESME_KILITSIZ_TAVAN" ]; then
  printf "  ✓ %-46s %s/%s kilitsiz (tavan %s)\n" "P-03: SOZLESME_KILITSIZ_TIP" \
    "$SOZLESME_KILITSIZ_TIP" "$SOZLESME_HEDEF" "$SOZLESME_KILITSIZ_TAVAN"
else
  printf "  ✗ %-46s %s/%s kilitsiz (tavan %s — cırcır YALNIZ azalır)\n" "P-03: SOZLESME_KILITSIZ_TIP" \
    "$SOZLESME_KILITSIZ_TIP" "$SOZLESME_HEDEF" "$SOZLESME_KILITSIZ_TAVAN"; HATA=1
fi

# <<< P-03

# >>> P-04 — ŞUBE BAĞLAMI + GLOBAL SORGU FİLTRESİ
#     (cırcır; PLAN § 2 P-04, kimlik Y-1, bağlı koşullar BK-02 / BK-09 / BK-20)
#
# NEDEN: şube izolasyonu ELLE `x.SubeId == subeId` yazılarak kurulursa bir
# sonraki uçta unutulur ve veri ya sızar (fail-open) ya kaybolur (fail-blind).
# Mekanizma tek yerdedir: KuyumcuDbContext'teki BİRLEŞİK (tenant AND sube)
# global sorgu filtresi + Data/SubeSuzgeci.cs (oturum şubesi) + yazma tarafında
# KiraciYazmaDenetcisi.SubeDenetle. Sınıflandırma tablosu:
#   dotnet/sube-suzgec-siniflandirma.txt   (a/b/c/d — kod değişiminden ÖNCE mühürlendi)
#
# ÖLÇÜM KOMUTLARI (BK-07 — tabanlar belgeden kopyalanmadı, burada taze koşuyor):
#   toplam nokta : grep -rh "SubeId ==" --include='*.cs' Kuyumcu.Api/Modules | wc -l
#   sınıfsız     : aynı liste | sed 's/^ *//' | grep -vFf <(tablodaki satır metinleri)
#   IQF          : grep -rh "\.IgnoreQueryFilters()" --include='*.cs' Kuyumcu.Api | wc -l
P04_TABLO="sube-suzgec-siniflandirma.txt"
if [ ! -f "$P04_TABLO" ]; then
  printf "  ✗ %-46s yok — sınıflandırma tablosu olmadan paket kapanmaz\n" "P-04: SINIFLANDIRMA_TABLOSU"; HATA=1
else
  P04_IZINLI=$(grep -v '^#' "$P04_TABLO" | cut -d'|' -f3- | sed 's/^[[:space:]]*//;s/[[:space:]]*$//' | grep -c .)
  P04_SATIRLAR=$(grep -rh "SubeId ==" --include='*.cs' "$API/Modules" 2>/dev/null | sed 's/^[[:space:]]*//;s/[[:space:]]*$//')
  P04_TOPLAM=$(printf '%s\n' "$P04_SATIRLAR" | grep -c .)
  GUVENLIK_ELLE_IZOLASYON=$(printf '%s\n' "$P04_SATIRLAR" | grep -c . >/dev/null; \
    printf '%s\n' "$P04_SATIRLAR" | grep -vFf <(grep -v '^#' "$P04_TABLO" | cut -d'|' -f3- | sed 's/^[[:space:]]*//;s/[[:space:]]*$//') | grep -c .)
  # KULLANICI_SUZGEC: yalnız RAPORLANIR (PLAN: azaltma hedefi YOK).
  KULLANICI_SUZGEC=$(grep -v '^#' "$P04_TABLO" | grep -c '^b|')
  printf "  ℹ %-46s %s (tablo: %s satır · sınıflandırılmış %s)\n" \
    "P-04: KULLANICI_SUZGEC (yalnız rapor)" "$KULLANICI_SUZGEC" "$P04_TOPLAM" "$P04_IZINLI"
  if [ "$GUVENLIK_ELLE_IZOLASYON" -eq 0 ]; then
    printf "  ✓ %-46s %s (-eq 0)\n" "P-04: GUVENLIK_ELLE_IZOLASYON" "$GUVENLIK_ELLE_IZOLASYON"
  else
    printf "  ✗ %-46s %s (-eq 0 olmalı — sınıflandırılmamış şube süzgeci)\n" \
      "P-04: GUVENLIK_ELLE_IZOLASYON" "$GUVENLIK_ELLE_IZOLASYON"
    printf '%s\n' "$P04_SATIRLAR" | grep -vFf <(grep -v '^#' "$P04_TABLO" | cut -d'|' -f3- | sed 's/^[[:space:]]*//;s/[[:space:]]*$//') | sed 's/^/      → /'
    HATA=1
  fi
fi

# IgnoreQueryFilters — PLAN tavanı 4; bugün ölçülen GERÇEK değer 0 (yalnız yorum
# satırlarında geçiyor). Tavan ARTIRILMAZ; azalan cırcır olarak 0'a sabitlendi.
IGNORE_QUERY_FILTERS_TAVAN=4
IGNORE_QUERY_FILTERS=$(grep -rh "\.IgnoreQueryFilters()" --include='*.cs' "$API" 2>/dev/null | grep -c .)
if [ "$IGNORE_QUERY_FILTERS" -le "$IGNORE_QUERY_FILTERS_TAVAN" ]; then
  printf "  ✓ %-46s %s (-le %s)\n" "P-04: IGNORE_QUERY_FILTERS" "$IGNORE_QUERY_FILTERS" "$IGNORE_QUERY_FILTERS_TAVAN"
else
  printf "  ✗ %-46s %s (-le %s olmalı)\n" "P-04: IGNORE_QUERY_FILTERS" "$IGNORE_QUERY_FILTERS" "$IGNORE_QUERY_FILTERS_TAVAN"; HATA=1
fi

# MEKANİZMA KAPISI: sube_id taşıyan her varlık ISubeVarlik uygulamalı (beyaz
# liste: Kullanici — kaydın değil, kullanıcının şubesi). Uygulama açılışta da
# denetler; burada derleme öncesi yakalanır.
P04_SUBE_ALAN=$(grep -rlE '\[Column\("sube_id"\)' --include='*.cs' "$API/Data" 2>/dev/null | wc -l | tr -d ' ')
P04_SUBE_VARLIK=$(grep -rh "IKiraciVarlik, ISubeVarlik" --include='*.cs' "$API/Data" 2>/dev/null | grep -c .)
# TABAN 18 = 17 varlık bildirimi + 1 jenerik kısıt (KiraciVeSubeFiltresiKur<T>).
# Taze ölçüm: grep -rh "IKiraciVarlik, ISubeVarlik" --include='*.cs' Kuyumcu.Api/Data | wc -l
if [ "$P04_SUBE_VARLIK" -ge 18 ]; then
  printf "  ✓ %-46s %s varlık (%s dosyada sube_id)\n" "P-04: SUBE_VARLIK_KAPSAM" "$P04_SUBE_VARLIK" "$P04_SUBE_ALAN"
else
  printf "  ✗ %-46s %s varlık (< 18 — ISubeVarlik kapsamı daraldı)\n" "P-04: SUBE_VARLIK_KAPSAM" "$P04_SUBE_VARLIK"; HATA=1
fi

# FAIL-OPEN KAPISI: birleşik filtre ve fail-closed kontrolü yerinde mi?
if grep -q "KiraciVeSubeFiltresiKur" "$API/Data/KuyumcuDbContext.cs" \
   && grep -q "Şube bağlamı çözülemedi" "$API/Data/KuyumcuDbContext.cs"; then
  printf "  ✓ %-46s birleşik filtre + fail-closed yerinde\n" "P-04: SUBE_FILTRE_MEKANIZMASI"
else
  printf "  ✗ %-46s birleşik filtre ya da fail-closed kontrolü YOK\n" "P-04: SUBE_FILTRE_MEKANIZMASI"; HATA=1
fi

# <<< P-04

# >>> P-05 — DOĞAL ANAHTAR BENZERSİZLİK KISITLARI
#     (cırcır; PLAN § 2 P-05, kimlik Y-2, bağlı koşul BK-21)
#
# NEDEN: "kod benzersizdir" cümlesi uygulama katmanındaki AnyAsync ön
# kontrolüyle korunuyorsa bir TEMENNİDİR; ön kontrol ile INSERT arasındaki
# pencerede ikinci istek satırı yazar. Kısıt VERİTABANINDA olacak.
#
# ÖLÇÜM KOMUTLARI (BK-07 — taban belgeden kopyalanmadı, burada taze koşar):
#   beklenen liste : bu betikteki P05_BEKLENEN dizisi (doğal anahtarların TEK kaynağı)
#   kısıtlı mı     : grep -A2 'HasDatabaseName("<ad>")' Kuyumcu.Api/Data/KuyumcuDbContext.cs | grep -c IsUnique
#   sözlük kaydı   : grep -c '"<ad>"' Kuyumcu.Core/I18n/BenzersizlikSozlugu.cs
#
# TABAN (2026-09-18, P-05 öncesi taze ölçüm): DOGAL_ANAHTAR_KISITSIZ = 4
#   (stoklar/urunler.kod/urunler.barkod/cariler.vergi_no kısıtsızdı;
#    cari_ibanlari zaten kısıtlıydı). HEDEF ve TAVAN: 0 — ARTIRILAMAZ.
P05_CTX="$API/Data/KuyumcuDbContext.cs"
P05_SOZLUK="Kuyumcu.Core/I18n/BenzersizlikSozlugu.cs"
P05_BEKLENEN="UQ_stoklar_tenant_sube_urun UQ_urunler_tenant_kod UQ_urunler_tenant_barkod UQ_cariler_tenant_vergi_no IX_cari_ibanlari_tenant_id_iban_hash"
DOGAL_ANAHTAR_KISITSIZ=0
P05_SOZLUKSUZ=0
for ad in $P05_BEKLENEN; do   # betik BASH — ${=VAR} zsh sözdizimidir, sessizce boş döner
  if [ "$ad" = "IX_cari_ibanlari_tenant_id_iban_hash" ]; then
    # P-05 ÖNCESİNDEN VAR: adı EF varsayılanıdır, HasDatabaseName taşımaz.
    VAR=$(grep -c 'CariIban>().HasIndex(c => new { c.TenantId, c.IbanHash }).IsUnique()' "$P05_CTX")
  else
    VAR=$(grep -A2 "HasDatabaseName(\"$ad\")" "$P05_CTX" 2>/dev/null | grep -c "IsUnique()")
  fi
  [ "$VAR" -ge 1 ] || DOGAL_ANAHTAR_KISITSIZ=$((DOGAL_ANAHTAR_KISITSIZ + 1))
  grep -q "\"$ad\"" "$P05_SOZLUK" 2>/dev/null || P05_SOZLUKSUZ=$((P05_SOZLUKSUZ + 1))
done

if [ "$DOGAL_ANAHTAR_KISITSIZ" -eq 0 ]; then
  printf "  ✓ %-46s %s (-eq 0)\n" "P-05: DOGAL_ANAHTAR_KISITSIZ" "$DOGAL_ANAHTAR_KISITSIZ"
else
  printf "  ✗ %-46s %s (-eq 0 olmalı — kısıtsız doğal anahtar)\n" \
    "P-05: DOGAL_ANAHTAR_KISITSIZ" "$DOGAL_ANAHTAR_KISITSIZ"; HATA=1
fi

# Her benzersiz kısıtın Türkçe ALAN ALTI karşılığı olmalı; yoksa kullanıcı
# MySQL'in İngilizce 1062 metnini ya da genel bir cümleyi görür.
if [ "$P05_SOZLUKSUZ" -eq 0 ]; then
  printf "  ✓ %-46s %s (-eq 0)\n" "P-05: BENZERSIZLIK_SOZLUKSUZ" "$P05_SOZLUKSUZ"
else
  printf "  ✗ %-46s %s (-eq 0 olmalı — sözlüksüz benzersiz kısıt)\n" \
    "P-05: BENZERSIZLIK_SOZLUKSUZ" "$P05_SOZLUKSUZ"; HATA=1
fi

# ── P-05 CANLI ŞEMA DOĞRULAMASI (kurul bulgusu, 2026-09-18) ──────────────────
# Yukarıdaki sayaç yalnız KAYNAK metnini okur. Kaynakta kısıt yazıp canlıya
# UYGULAMAMAK (bekleyen migration) "yeşil yalan" üretiyordu: 4/5 kısıtlı bir
# üretim veritabanı bile TEMİZ görünüyordu. Bu blok aynı adları CANLI
# information_schema'dan sorar. Bağlantı yoksa SESSİZ YEŞİL vermez, YAPILAMADI
# damgası basar.
#   ÖLÇÜM: docker exec kuyumcu-mysql mysql -N -e "select INDEX_NAME,NON_UNIQUE
#          from information_schema.STATISTICS where TABLE_SCHEMA='kuyumcu_net'"
#   NOT  : UQ_cariler_tenant_vergi_no FONKSİYONEL indekstir (CASE WHEN aktif…);
#          STATISTICS'te COLUMN_NAME boş, EXPRESSION dolu gelir — ad ve
#          NON_UNIQUE=0 kontrolü yine geçerlidir.
P05_CANLI_EKSIK=0
P05_CANLI_OLCULDU=0
if command -v docker >/dev/null 2>&1 && docker ps --format '{{.Names}}' 2>/dev/null | grep -qx "kuyumcu-mysql"; then
  P05_PAROLA=$(awk -F': ' '/^[[:space:]]*MYSQL_ROOT_PASSWORD:/ {print $2; exit}' ../docker-compose.yml 2>/dev/null)
  if [ -n "${P05_PAROLA:-}" ]; then
    P05_CANLI=$(docker exec kuyumcu-mysql mysql -uroot -p"$P05_PAROLA" -N -e \
      "select distinct INDEX_NAME from information_schema.STATISTICS \
       where TABLE_SCHEMA='kuyumcu_net' and NON_UNIQUE=0;" 2>/dev/null)
    if [ -n "$P05_CANLI" ]; then
      P05_CANLI_OLCULDU=1
      for ad in $P05_BEKLENEN; do
        printf '%s\n' "$P05_CANLI" | grep -qx "$ad" || {
          P05_CANLI_EKSIK=$((P05_CANLI_EKSIK + 1))
          printf "      → canlıda YOK: %s\n" "$ad"; }
      done
    fi
  fi
fi
if [ "$P05_CANLI_OLCULDU" -eq 0 ]; then
  printf "  ⚠ %-46s %s\n" "P-05: CANLI_KISIT_DOGRULAMASI" "YAPILAMADI (kuyumcu-mysql yok — sessiz yeşil DEĞİL)"
elif [ "$P05_CANLI_EKSIK" -eq 0 ]; then
  printf "  ✓ %-46s %s (-eq 0)\n" "P-05: CANLI_KISITSIZ" "$P05_CANLI_EKSIK"
else
  printf "  ✗ %-46s %s (-eq 0 olmalı — kaynakta var, CANLIDA yok: bekleyen migration?)\n" \
    "P-05: CANLI_KISITSIZ" "$P05_CANLI_EKSIK"; HATA=1
fi

# ── P-05 İNDEKS İFADESİ DOĞRULAMASI (kurul tur 3 bulgusu, 2026-09-18) ────────
# NEDEN: UQ_cariler_tenant_vergi_no'nun canlıdaki gerçek DDL'i FONKSİYONELDİR
# (tenant_id, (CASE WHEN aktif=1 THEN vergi_no END)); EF modeli ise onu DÜZ
# unique olarak bildirir (Pomelo filtreli indeksi modelleyemez). Ad + NON_UNIQUE
# kontrolü bu farkı GÖRMEZ: birisi indeksi model üzerinden yeniden üretirse
# (EnsureCreated, "düzeltici" bir diff migration) aynı adla DÜZ bir kısıt kurulur,
# PASİF+AKTİF meşru çifti reddedilmeye başlar ve denetim yine yeşil yanar.
# Bu sayaç indeksin İFADESİNİ sorar; düz indekse sessiz geri dönüşü yakalar.
#   ÖLÇÜM: docker exec kuyumcu-mysql mysql -N -e "select coalesce(EXPRESSION,'')
#          from information_schema.STATISTICS where TABLE_SCHEMA='kuyumcu_net'
#          and INDEX_NAME='UQ_cariler_tenant_vergi_no'"
#   TABAN (2026-09-18 taze ölçüm): İFADESİZ = 0 hedef; kısıt canlıya uygulanana
#          dek ölçüm YAPILAMADI damgası basar (sessiz yeşil değil).
P05_IFADESIZ=0
P05_IFADE_OLCULDU=0
if [ "$P05_CANLI_OLCULDU" -eq 1 ] && [ -n "${P05_PAROLA:-}" ]; then
  # Önce indeks CANLIDA VAR MI? (yoksa ölçüm YAPILAMADI'dır — "düz indeks" değil)
  P05_IFADE_VARMI=$(docker exec kuyumcu-mysql mysql -uroot -p"$P05_PAROLA" -N -e \
    "select count(*) from information_schema.STATISTICS \
     where TABLE_SCHEMA='kuyumcu_net' and INDEX_NAME='UQ_cariler_tenant_vergi_no';" 2>/dev/null)
  if [ "${P05_IFADE_VARMI:-0}" -ge 1 ] 2>/dev/null; then
    P05_IFADE_OLCULDU=1
    # İndeks VAR: parçalarından en az biri 'aktif' geçen bir İFADE taşımalı.
    # Taşımıyorsa indeks DÜZ demektir → ihlal (sessiz geri dönüş yakalandı).
    P05_IFADE=$(docker exec kuyumcu-mysql mysql -uroot -p"$P05_PAROLA" -N -e \
      "select coalesce(EXPRESSION,'') from information_schema.STATISTICS \
       where TABLE_SCHEMA='kuyumcu_net' and INDEX_NAME='UQ_cariler_tenant_vergi_no';" 2>/dev/null)
    printf '%s\n' "${P05_IFADE:-}" | grep -qi "aktif" || P05_IFADESIZ=1
  fi
fi
if [ "$P05_IFADE_OLCULDU" -eq 0 ]; then
  printf "  ⚠ %-46s %s\n" "P-05: INDEKS_IFADESI" "YAPILAMADI (canlı kısıt yok/ölçülemedi — sessiz yeşil DEĞİL)"
elif [ "$P05_IFADESIZ" -eq 0 ]; then
  printf "  ✓ %-46s %s (-eq 0)\n" "P-05: INDEKS_IFADESIZ" "$P05_IFADESIZ"
else
  printf "  ✗ %-46s %s (-eq 0 olmalı — fonksiyonel indeks DÜZ indekse dönmüş)\n" \
    "P-05: INDEKS_IFADESIZ" "$P05_IFADESIZ"; HATA=1
fi

# MEKANİZMA KAPISI: 1062 → 409 çevirisi boru hattında duruyor mu?
if grep -q "BenzersizlikIhlaliMi" "$API/Common/ApiException.cs" \
   && grep -q "AlanCakismasiException" "$API/Common/ApiException.cs"; then
  printf "  ✓ %-46s 1062→409 + alan altı yerinde\n" "P-05: BENZERSIZLIK_BORU_HATTI"
else
  printf "  ✗ %-46s 1062→409 çevirisi ya da alan altı 409 YOK\n" "P-05: BENZERSIZLIK_BORU_HATTI"; HATA=1
fi

# <<< P-05

# >>> P-06 — DÜZENLENEBİLİRLİK ŞABLONU SAYAÇLARI
#     (cırcır; PLAN § 2 P-06, kimlikler B-4 · B-13 · B-18 · B-34 · B-54 · Y-15,
#      P-00 triyaj temaları B-60 · B-67 · B-68 · B-70; UX-S25/S28/S29/S31)
#
# İLKE (PLAN § 1 md.2): buradaki hiçbir taban belgeden kopyalanmadı. Her sayacın
# ÖLÇÜM KOMUTU yorum olarak yazılıdır ve tabanlar 2026-09-18'de TAZE ölçüldü.
# Plan "taban P-02'de ölçülen değerdir" diyordu; P-02 bloğunda DUZENLENEMEYEN_GIRIS
# sayacı YOKTUR (grep ile doğrulandı) — bu yüzden taban burada ölçüldü, sapma
# uygulama raporunda yazılıdır.
#
# ⚠ CIRCIR: tavanlar yalnız AZALIR. Bir sayfayı istisna kütüğüne yazarak borcu
#   saklamak P06-2'yi kırar; kütük tavanı da ayrıca cırcırdır (P06-3).
echo
echo "═══ P-06 düzenlenebilirlik şablonu ═══"

P06_PAGES="$WEB/Pages"
P06_BIL="$WEB/Bilesenler"
p06_ok()  { printf "  ✓ %-46s %s\n" "$1" "$2"; }
p06_kir() { printf "  ✗ %-46s %s\n" "$1" "$2"; HATA=1; }

# ── P06-1) DUZENLENEMEYEN_GIRIS (cırcır, AZALAN) ─────────────────────────────
#   TANIM: yazma yapan (.PostAsync/.PutAsync/.PatchAsync/.DeleteAsync) bir
#   Pages/*.razor dosyasının ne mühürlü düzenleme kalıbı (SatirIciDuzenle ·
#   Cekmece · DuzenlenebilirListe · DegismezIpucu) ne de makineyle doğrulanan
#   `data-duzenleme-yolu` beyanı taşıması.
#   ⚠ --include='*.razor' / -name '*.razor' ZORUNLU (P-02 R18): .mutorj artıkları
#     grep'e karışırsa sayaç kirlenir.
#   ÖLÇÜM (2026-09-18 tazesi → 3; P-06 sonrası → 3, hepsi KÜTÜKLÜ istisna):
#     for f in Kuyumcu.Web/Pages/*.razor; do ... done   (aşağıdaki döngünün aynısı)
P06_TAVAN_KALIPSIZ=3
P06_KALIPSIZ=0
P06_KALIPSIZ_LISTE=""
for f in "$P06_PAGES"/*.razor; do
  grep -qE '\.(PostAsync|PutAsync|PatchAsync|DeleteAsync)\b' "$f" || continue
  if grep -qE '<(SatirIciDuzenle|Cekmece|DuzenlenebilirListe|DegismezIpucu)' "$f"; then continue; fi
  if grep -qE 'data-duzenleme-yolu="[A-Za-z0-9]+\.razor"' "$f"; then continue; fi
  P06_KALIPSIZ=$((P06_KALIPSIZ + 1))
  P06_KALIPSIZ_LISTE="$P06_KALIPSIZ_LISTE $(basename "$f")"
done
if [ "$P06_KALIPSIZ" -le "$P06_TAVAN_KALIPSIZ" ]; then
  p06_ok "P-06: DUZENLENEMEYEN_GIRIS" "$P06_KALIPSIZ (tavan $P06_TAVAN_KALIPSIZ, cırcır)"
else
  p06_kir "P-06: DUZENLENEMEYEN_GIRIS" "$P06_KALIPSIZ > $P06_TAVAN_KALIPSIZ →$P06_KALIPSIZ_LISTE"
fi

# ── P06-2) DUZENLEME_KALIBI_YOK_SAYFA = 0 ────────────────────────────────────
#   Kalıpsız sayfaların HEPSİ istisna kütüğünde (DuzeltmePolitikasi.EkranIstisnalari)
#   yazılı olmalıdır. Kütüksüz tek sayfa bile sayacı kırar — "sessiz kalıpsızlık"
#   kabul edilmez (Anayasa md.1, UX-S25).
#   ÖLÇÜM: yukarıdaki listenin her adı için
#          grep -c 'new("<ad>"' Kuyumcu.Api/Modules/Meta/DuzeltmePolitikasi.cs
P06_KUTUK="$API/Modules/Meta/DuzeltmePolitikasi.cs"
P06_KUTUKSUZ=0
P06_KUTUKSUZ_LISTE=""
# ⚠ zsh, tırnaksız değişkeni BOŞLUKTAN BÖLMEZ; ${=VAR} şarttır. Bu satır
#   ${=} olmadan listeyi TEK ad sayıp kütükte bulamıyor ve YANLIŞ KIRMIZI
#   veriyordu (P-30 doğrulamasında saptandı; eşik/tavan değişmedi).
for ad in $(echo "$P06_KALIPSIZ_LISTE"); do
  if ! grep -q "new(\"$ad\"" "$P06_KUTUK"; then
    P06_KUTUKSUZ=$((P06_KUTUKSUZ + 1))
    P06_KUTUKSUZ_LISTE="$P06_KUTUKSUZ_LISTE $ad"
  fi
done
if [ "$P06_KUTUKSUZ" -eq 0 ]; then
  p06_ok "P-06: DUZENLEME_KALIBI_YOK_SAYFA" "0 (-eq 0)"
else
  p06_kir "P-06: DUZENLEME_KALIBI_YOK_SAYFA" "$P06_KUTUKSUZ ≠ 0 →$P06_KUTUKSUZ_LISTE"
fi

# ── P06-3) ISTISNA_KUTUK_TAVAN (cırcır, AZALAN) ──────────────────────────────
#   B-54: gerekçeli istisnalar paydadan düşülür AMA sayıları ARTAMAZ.
#   ÖLÇÜM: grep -c 'new("[A-Za-z]*\.razor"' Kuyumcu.Api/Modules/Meta/DuzeltmePolitikasi.cs → 3
P06_ISTISNA_TAVAN=3
P06_ISTISNA=$(grep -cE 'new\("[A-Za-z0-9]+\.razor"' "$P06_KUTUK" 2>/dev/null | tr -d ' ')
if [ "${P06_ISTISNA:-0}" -le "$P06_ISTISNA_TAVAN" ]; then
  p06_ok "P-06: ISTISNA_KUTUK_TAVAN" "$P06_ISTISNA (tavan $P06_ISTISNA_TAVAN, cırcır)"
else
  p06_kir "P-06: ISTISNA_KUTUK_TAVAN" "$P06_ISTISNA > $P06_ISTISNA_TAVAN — tavan ARTIRILMAZ"
fi

# ── P06-4) ALAN_ANAHTARSIZ_DOGRULAMA = 0 ─────────────────────────────────────
#   "İşaretli alanları düzeltin" türü, hangi alana ait olduğu DOM'dan okunamayan
#   toplu doğrulama mesajı YASAK (PLAN P-06 DoD md.5). Tarama iki yönlüdür:
#   (a) yasak kalıp metni, (b) SatirIciDuzenle'nin alan anahtarlı hata id'si.
#   ÖLÇÜM:
#     grep -rniE 'işaretli alanlar|isaretli alanlar|formu kontrol edin' Kuyumcu.Web --include='*.razor' | wc -l → 0
P06_TOPLU=$(grep -rniE 'işaretli alanlar|isaretli alanlar|formu kontrol edin|alanları düzeltin' \
  "$WEB" --include='*.razor' --include='*.cs' 2>/dev/null | wc -l | tr -d ' ')
P06_ANAHTARLI=0
grep -q 'si-{Varlik}-{Alan}-{Id}-hata' "$P06_BIL/SatirIciDuzenle.razor" && P06_ANAHTARLI=1
grep -q 'aria-describedby' "$P06_BIL/SatirIciDuzenle.razor" || P06_ANAHTARLI=0
if [ "$P06_TOPLU" -eq 0 ] && [ "$P06_ANAHTARLI" -eq 1 ]; then
  p06_ok "P-06: ALAN_ANAHTARSIZ_DOGRULAMA" "0 (-eq 0; hata id alan anahtarlı + aria-describedby)"
else
  p06_kir "P-06: ALAN_ANAHTARSIZ_DOGRULAMA" "toplu_mesaj=$P06_TOPLU alan_anahtarli_hata_id=$P06_ANAHTARLI"
fi

# ── P06-5) BU_PAKETTE_EKLENEN_RECORD = 0 ─────────────────────────────────────
#   PLAN § 1 md.9: Pages/Bilesenler/Layout record tavanı yalnız AZALIR. Bu paket
#   iki dosyaya dokundu ve HİÇBİRİNE record eklemedi (şablon jeneriktir).
#   ÖLÇÜM: grep -c '\brecord ' Bilesenler/DuzenlenebilirListe.razor Bilesenler/SatirIciDuzenle.razor → 0
P06_RECORD=$(grep -hcE '\brecord ' "$P06_BIL/DuzenlenebilirListe.razor" "$P06_BIL/SatirIciDuzenle.razor" 2>/dev/null \
  | awk '{t+=$1} END{print t+0}')
if [ "$P06_RECORD" -eq 0 ]; then
  p06_ok "P-06: BU_PAKETTE_EKLENEN_RECORD" "0 (-eq 0)"
else
  p06_kir "P-06: BU_PAKETTE_EKLENEN_RECORD" "$P06_RECORD ≠ 0 — ortak tip Kuyumcu.Contracts'a konur"
fi

# ── P06-6) MEKANİZMA KAPISI: şablonun kendisi duruyor mu? ────────────────────
#   Sayaçlar şablona bakar; şablon boşaltılırsa sayaçlar yanlış yeşil verirdi.
P06_SABLON=1
grep -q 'throw new InvalidOperationException' "$P06_BIL/DuzenlenebilirListe.razor" || P06_SABLON=0
grep -q 'girdiRef.FocusAsync()' "$P06_BIL/SatirIciDuzenle.razor" || P06_SABLON=0
grep -q 'AralikHatasi()' "$P06_BIL/SatirIciDuzenle.razor" || P06_SABLON=0
if [ "$P06_SABLON" -eq 1 ]; then
  p06_ok "P-06: SABLON_BUTUNLUGU" "DuzenlenebilirListe kapısı + odak + aralık yerinde"
else
  p06_kir "P-06: SABLON_BUTUNLUGU" "şablonun zorunlu parçalarından biri BOŞALTILMIŞ"
fi

# <<< P-06

# >>> P-07 — API DAYANIKLILIK BANDI SAYAÇLARI
#     (cırcır; PLAN § 2 P-07, kimlikler B-10 · B-57; UX-S28 · UX-S30)
#
# İLKE (PLAN § 1 md.2): tabanlar belgeden KOPYALANMADI, 2026-09-18'de TAZE ölçüldü.
# Plan "bugün ölçülen 4 tüketici" diyordu; bugünkü ölçüm ApiIstemci ve Program.cs
# DIŞINDA 1 gerçek tüketici buldu (OturumServisi) — ArayuzModuServisi ve
# BasitSatisAkisi'ndaki "HttpClient" geçişleri YORUM satırıdır. Sapma uygulama
# raporunda yazılıdır. P-07 sonrası ölçüm: 0.
#
# ⚠ CIRCIR: tavanlar yalnız AZALIR.
echo
echo "═══ P-07 API dayanıklılık bandı ═══"

P07_ISTEMCI="$WEB/Servisler/ApiIstemci.cs"
p07_ok()  { printf "  ✓ %-46s %s\n" "$1" "$2"; }
p07_kir() { printf "  ✗ %-46s %s\n" "$1" "$2"; HATA=1; }

# ── P07-1) APIISTEMCI_DISI_HTTPCLIENT = 0 ────────────────────────────────────
#   TANIM: Kuyumcu.Web'de ApiIstemci.cs ve Program.cs (DI kaydı, beyaz liste)
#   dışında HttpClient'ı KOD olarak tüketen satır. Yorumdaki "HttpClient"
#   sözcüğü sayılmaz: desen tip kullanımını arar (kurucu parametresi,
#   `new HttpClient`, `<HttpClient>`).
#   Neden: her istek 429 geri çekilmesi, fırtına kapısı ve tek uçuşlu jeton
#   yenilemesinden GEÇMELİDİR; ikinci bir HTTP yolu bandı deler.
#   ÖLÇÜM (2026-09-18 tazesi → 0):
#     grep -rnE '(\(|,[[:space:]]*)HttpClient[[:space:]]+[A-Za-z_]|new[[:space:]]+HttpClient|<HttpClient>' \
#       --include='*.cs' --include='*.razor' Kuyumcu.Web \
#       | grep -v 'Servisler/ApiIstemci.cs' | grep -v 'Kuyumcu.Web/Program.cs' | wc -l
#   ⚠ BEYAZ LİSTE — Servisler/HavaServisi.cs (P-37b, 2026-09-21): hava DIŞ BİR
#     SUNUCUYA gider. ApiIstemci bandı her isteğe kiracı JWT'sini ekler; hava
#     isteğini o banttan geçirmek OTURUM JETONUNU ÜÇÜNCÜ TARAFA GÖNDERMEK olur.
#     Bu yüzden ayrı, kısa zaman aşımlı ve JETONSUZ bir HttpClient KURAL GEREĞİ
#     doğrudur. Kapsamı HAVA_BEKLETME_KAPISI sayacı ayrıca kilitler.
P07_DISI=$(grep -rnE '(\(|,[[:space:]]*)HttpClient[[:space:]]+[A-Za-z_]|new[[:space:]]+HttpClient|<HttpClient>' \
  --include='*.cs' --include='*.razor' "$WEB" 2>/dev/null \
  | grep -v 'Servisler/ApiIstemci.cs' | grep -v "$WEB/Program.cs" \
  | wc -l | tr -d ' ')
#   ⚠ S1-j (24.09): HavaServisi BEYAZ LİSTEDEN ÇIKTI — hava artık kendi
#     sunucumuzdan (/api/v1/hava, ApiIstemci bandı) gelir; jeton üçüncü tarafa
#     GİTMEZ çünkü dış çağrıyı SUNUCU yapar. İstemcide ikinci HTTP yolu 0.
if [ "${P07_DISI:-0}" -eq 0 ]; then
  p07_ok "P-07: APIISTEMCI_DISI_HTTPCLIENT" "0 (-eq 0)"
else
  p07_kir "P-07: APIISTEMCI_DISI_HTTPCLIENT" "$P07_DISI ≠ 0 — her istek ApiIstemci bandından geçmeli"
fi

# ── P07-2) ISTEK_TEKRAR_TAVANI (cırcır, AZALAN) ──────────────────────────────
#   B-57: tek sayfa ömründe aynı uca atılabilecek azami istek. Tavan
#   ARTIRILAMAZ — fırtınayı gizlemenin en kolay yolu tavanı yükseltmektir.
#   ÖLÇÜM (2026-09-18 tazesi → 60):
#     grep -oE 'IstekTekrarTavani = [0-9]+' Kuyumcu.Web/Servisler/ApiIstemci.cs
P07_TAVAN_AZAMI=60
P07_TAVAN=$(grep -oE 'IstekTekrarTavani = [0-9]+' "$P07_ISTEMCI" 2>/dev/null | grep -oE '[0-9]+' | head -1)
if [ -n "$P07_TAVAN" ] && [ "$P07_TAVAN" -le "$P07_TAVAN_AZAMI" ]; then
  p07_ok "P-07: ISTEK_TEKRAR_TAVANI" "$P07_TAVAN (azami $P07_TAVAN_AZAMI, cırcır)"
else
  p07_kir "P-07: ISTEK_TEKRAR_TAVANI" "${P07_TAVAN:-YOK} > $P07_TAVAN_AZAMI ya da sabit SİLİNMİŞ"
fi

# ── P07-3) BANT_BUTUNLUGU ────────────────────────────────────────────────────
#   Bandın parçaları BOŞALTILAMAZ: 429 dalı, üstel geri çekilme + jitter,
#   fırtına kapısı, tek uçuşlu jeton yenileme ve sunucu tarafı oran sınırı.
#   ÖLÇÜM: aşağıdaki grep'lerin HEPSİ eşleşmeli.
P07_BUTUN=1
grep -q 'HttpStatusCode.TooManyRequests' "$P07_ISTEMCI" || P07_BUTUN=0
grep -q 'GeriCekilmeSuresi' "$P07_ISTEMCI"              || P07_BUTUN=0
grep -q 'Jitter(JitterMs)' "$P07_ISTEMCI"               || P07_BUTUN=0
grep -q 'FirtinaKontrol' "$P07_ISTEMCI"                 || P07_BUTUN=0
grep -q 'TekUcuslaYenileAsync' "$P07_ISTEMCI"           || P07_BUTUN=0
grep -q 'Yeniden dene' "$P07_ISTEMCI"                   || P07_BUTUN=0
grep -q 'AddRateLimiter' "$API/Common/OranSiniri.cs"    || P07_BUTUN=0
if [ "$P07_BUTUN" -eq 1 ]; then
  p07_ok "P-07: BANT_BUTUNLUGU" "429 dalı + geri çekilme/jitter + fırtına kapısı + tek uçuş"
else
  p07_kir "P-07: BANT_BUTUNLUGU" "bandın zorunlu parçalarından biri BOŞALTILMIŞ"
fi

# <<< P-07

# >>> P-08 — HAREKET TÜRÜ SÖZLEŞMESİ + ÇEKİRDEK KOLONLAR + FATURA BAĞI
#     (cırcır; PLAN § 2 P-08, kimlikler Y-32 · Y-14 · Y-13 · Y-25 · B-52;
#      BK-04 → dokumanlar/02-mimari/adr/0018-fatura-hareket-bagi-tek-yon.md)
#
# İLKE (PLAN § 1 md.2 + BK-07): tabanlar belgeden KOPYALANMADI, 2026-09-18'de
# TAZE ölçüldü. Her sayacın ÖLÇÜM KOMUTU aşağıda yorum olarak yazılıdır.
#
# ⚠ CIRCIR: tavanlar yalnız AZALIR.
echo
echo "═══ P-08 hareket türü sözleşmesi + fatura bağı ═══"

P08_FABRIKA="$API/Data/HareketFabrikasi.cs"
P08_BAG="$API/Data/FaturaHareketBagi.cs"
p08_ok()  { printf "  ✓ %-46s %s\n" "$1" "$2"; }
p08_kir() { printf "  ✗ %-46s %s\n" "$1" "$2"; HATA=1; }

# ── P08-1) HARETURU_DISI_SABIT = 0 ───────────────────────────────────────────
#   TANIM: Kuyumcu.Api'de `Tur = "XXX"` biçiminde yazılmış her hareket türü
#   sabiti, Sabitler.HareketTurleri kapalı listesinde BULUNMALIDIR.
#   Neden: liste 2026-09-15'e kadar yalnız BELGE değeri taşıyordu ("bu dizi
#   kodda ZORLANMAZ" — Entities.cs). Listede olmayan bir tür deftere düşerse
#   rapor/denetim tarafı onu "geçersiz tür" sanıp SÜZER; ciro eksik çıkar.
#   ÖLÇÜM (2026-09-18 tazesi → 0):
#     grep -rhoE 'Tur[[:space:]]*=[[:space:]]*"[A-Z_]+"' --include='*.cs' Kuyumcu.Api \
#       | grep -oE '"[A-Z_]+"' | tr -d '"' | sort -u   → her biri HareketTurleri'nde mi
P08_LISTE=$(sed -n '/HareketTurleri =/,/\];/p' "$API/Data/Entities.cs" | grep -oE '"[A-Z_]+"' | tr -d '"' | sort -u)
P08_KULLANILAN=$(grep -rhoE 'Tur[[:space:]]*=[[:space:]]*"[A-Z_]+"' --include='*.cs' "$API" 2>/dev/null \
  | grep -oE '"[A-Z_]+"' | tr -d '"' | sort -u)
P08_DISI=0
P08_DISI_AD=""
for t in $P08_KULLANILAN; do
  if ! echo "$P08_LISTE" | grep -qx "$t"; then P08_DISI=$((P08_DISI+1)); P08_DISI_AD="$P08_DISI_AD $t"; fi
done
if [ "$P08_DISI" -eq 0 ]; then
  p08_ok "P-08: HARETURU_DISI_SABIT" "0 (-eq 0)"
else
  p08_kir "P-08: HARETURU_DISI_SABIT" "$P08_DISI ≠ 0 —$P08_DISI_AD listede yok"
fi

# ── P08-2) TUR_KAPISI (Y-32 — liste BELGE değil KAPI olmalı) ─────────────────
#   Sayaç tek başına yetmez: liste kodda ZORLANMIYORSA türü DIŞARIDAN gelen
#   yazma noktaları (AcilisServisi `Tur = tur`, StornoServisi `Tur = kaynak.Tur`,
#   IsCalistirici `Tur = taslak.Tur`) sabit grep'ine hiç yakalanmaz.
#   ÖLÇÜM: HareketFabrikasi.Ekle içinde kapalı liste denetimi + 400 dalı.
P08_KAPI=1
grep -q 'Sabitler.HareketTurleri' "$P08_FABRIKA"        || P08_KAPI=0
grep -q 'Tanımsız hareket türü' "$P08_FABRIKA"          || P08_KAPI=0
grep -q 'BadRequestException' "$P08_FABRIKA"            || P08_KAPI=0
grep -q 'Hareket adedi negatif olamaz' "$P08_FABRIKA"   || P08_KAPI=0
grep -q 'PersonelId boş dizge olamaz' "$P08_FABRIKA"    || P08_KAPI=0
if [ "$P08_KAPI" -eq 1 ]; then
  p08_ok "P-08: TUR_KAPISI + ADET/PERSONEL KAPISI" "HareketFabrikasi'nda kapı, 400 dalı"
else
  p08_kir "P-08: TUR_KAPISI + ADET/PERSONEL KAPISI" "kapı BOŞALTILMIŞ — liste yine yalnız belge değeri"
fi

# ── P08-3) HAREKETFABRIKASI_DISI_YAZMA = 0 ───────────────────────────────────
#   TANIM: `db.Hareketler.Add(` çağrısı Kuyumcu.Api'de YALNIZ
#   Data/HareketFabrikasi.cs içinde olabilir. Yorum satırları sayılmaz.
#   ÖLÇÜM (2026-09-18 tazesi → 0):
#     grep -rnE '(^|[^/[:alnum:]])[A-Za-z_]+\.Hareketler\.Add(Range)?\(' --include='*.cs' Kuyumcu.Api \
#       | grep -v 'Data/HareketFabrikasi.cs' | grep -vE ':[[:space:]]*//' | wc -l
P08_YAZMA=$(grep -rnE '(^|[^/[:alnum:]])[A-Za-z_]+\.Hareketler\.Add(Range)?\(' --include='*.cs' "$API" 2>/dev/null \
  | grep -v 'Data/HareketFabrikasi.cs' | grep -vE ':[[:space:]]*//' | wc -l | tr -d ' ')
if [ "${P08_YAZMA:-0}" -eq 0 ]; then
  p08_ok "P-08: HAREKETFABRIKASI_DISI_YAZMA" "0 (-eq 0)"
else
  p08_kir "P-08: HAREKETFABRIKASI_DISI_YAZMA" "$P08_YAZMA ≠ 0 — deftere tek yol HareketFabrikasi.Ekle"
fi

# ── P08-4) FATURA_NO_SERBEST_METIN (cırcır, AZALAN) ──────────────────────────
#   TANIM (Y-25 / ADR-0018): `hareketler.fatura_no` metnini BAĞ KURMAK için
#   okuyan satır. Damgayı YAZAN satır (`h.FaturaNo = ...`) sayılmaz; salt
#   gösterim projeksiyonu da sayılmaz (karşılaştırma operatörü aranır).
#   Data/FaturaHareketBagi.cs TEK SORGU YOLU'dur, paydadan düşülür.
#   ÖLÇÜLEN TABAN: P-08 ÖNCESİ 7 → P-08 SONRASI 2. Tavan 2.
#   KALAN 2 SATIR (adıyla, gizlenmeden):
#     · RaporModulu.cs "belgesiz hurda" — orada fatura_no BİZİM belgemiz değil
#       TEDARİKÇİNİN fatura numarasıdır; soru "bağ" değil "belge var mı"dır.
#     · SatisModulu.cs fiş okuma — salt GÖSTERİM projeksiyonu, bağ kurmaz.
#   ÖLÇÜM:
#     grep -rnE 'h\.FaturaNo' --include='*.cs' Kuyumcu.Api | grep -v 'h\.FaturaNo = ' \
#       | grep -E 'IsNullOrEmpty|Contains|==|!=' | grep -v 'Data/FaturaHareketBagi.cs' | wc -l
P08_FNO_AZAMI=2
P08_FNO=$(grep -rnE 'h\.FaturaNo' --include='*.cs' "$API" 2>/dev/null \
  | grep -v 'h\.FaturaNo = ' | grep -E 'IsNullOrEmpty|Contains|==|!=' \
  | grep -v 'Data/FaturaHareketBagi.cs' | wc -l | tr -d ' ')
if [ "${P08_FNO:-99}" -le "$P08_FNO_AZAMI" ]; then
  p08_ok "P-08: FATURA_NO_SERBEST_METIN" "$P08_FNO (azami $P08_FNO_AZAMI, cırcır)"
else
  p08_kir "P-08: FATURA_NO_SERBEST_METIN" "$P08_FNO > $P08_FNO_AZAMI — bağ TEK YOLDAN okunur (ADR-0018)"
fi

# ── P08-5) BAG_BUTUNLUGU (Y-25 / BK-04) ──────────────────────────────────────
#   Tek sorgu yolu ve ADR BOŞALTILAMAZ; ters yön kolonu (hareketler.fatura_id)
#   AÇILAMAZ (iki paralel bağ = mutabakatta çifte sayma).
P08_BUTUN=1
[ -f "$P08_BAG" ]                                       || P08_BUTUN=0
grep -q 'KaynakKalem' "$P08_BAG"                        || P08_BUTUN=0
grep -q 'FaturaKalemleri' "$P08_BAG"                    || P08_BUTUN=0
grep -q 'FaturaHareketBagi' "$API/Modules/Rapor/RaporModulu.cs"   || P08_BUTUN=0
grep -q 'FaturaHareketBagi' "$API/Modules/Fatura/FaturaModulu.cs" || P08_BUTUN=0
[ -f "../dokumanlar/02-mimari/adr/0018-fatura-hareket-bagi-tek-yon.md" ] || P08_BUTUN=0
# Ters yön kolonu AÇILMAMIŞ olmalı. ⚠ Kapsam YALNIZ Hareket sınıfının gövdesidir:
# banka_hareketleri.fatura_id MEVCUTTUR ve MEŞRUDUR (dekont ↔ fatura eşleştirmesi),
# yasak olan hareketler.fatura_id'dir.
P08_HAREKET_GOVDE=$(sed -n '/^public class Hareket : IKiraciVarlik/,/^\[Table("kasalar")\]/p' "$API/Data/Entities.cs")
if echo "$P08_HAREKET_GOVDE" | grep -qE 'Column\("fatura_id"\)'; then P08_BUTUN=0; fi
if [ "$P08_BUTUN" -eq 1 ]; then
  p08_ok "P-08: BAG_BUTUNLUGU" "tek sorgu yolu + ADR-0018 + ters yön kolonu YOK"
else
  p08_kir "P-08: BAG_BUTUNLUGU" "tek sorgu yolu / ADR-0018 boşaltılmış ya da hareketler.fatura_id açılmış"
fi

# ── P08-6) PERSONEL_ANLAM_KILIDI (Y-13) ──────────────────────────────────────
#   personel_id kolonunun TEK anlamı "SATAN"dır; sabit ve kolon birlikte durur.
P08_PERS=1
grep -q 'HareketPersonelRolu = "SATAN"' "$API/Data/Entities.cs"      || P08_PERS=0
grep -q 'Column("personel_id")' "$API/Data/Entities.cs"              || P08_PERS=0
grep -q 'Column("adet", TypeName = "decimal(12,3)")] public decimal? Adet' "$API/Data/Entities.cs" || P08_PERS=0
grep -q 'h.PersonelId, h.Tarih' "$API/Data/KuyumcuDbContext.cs"      || P08_PERS=0
if [ "$P08_PERS" -eq 1 ]; then
  p08_ok "P-08: PERSONEL_ANLAM_KILIDI + ADET" "SATAN sabiti + nullable adet + indeks"
else
  p08_kir "P-08: PERSONEL_ANLAM_KILIDI + ADET" "kolon/sabit/indeks BOŞALTILMIŞ"
fi

# <<< P-08

# >>> P-09 — KİMLİK, ERİŞİM, MFA VE YETKİ MATRİSİ
#     (cırcır; PLAN § 2 P-09, kimlikler B-6 · B-20 · B-35 · B-36 · B-37 · Y-39
#      + Ramazan K3 dilimi; BK-10 · BK-13 · BK-22 · BK-35)
#
# İLKE (PLAN § 1 md.2 + BK-07): tabanlar belgeden KOPYALANMADI, 2026-09-20'de
# TAZE ölçüldü. Her sayacın ÖLÇÜM KOMUTU aşağıda yorum olarak yazılıdır.
#
# ⚠ CIRCIR: tavanlar yalnız AZALIR. Dördü de 0'dır; 0'ın altı yoktur.
echo
echo "═══ P-09 kimlik, erişim, MFA ve yetki matrisi ═══"

P09_ROTAHARITA="Kuyumcu.Core/Erisim/RotaModulleri.cs"
P09_KUL="$API/Modules/Kullanici/KullaniciModulu.cs"
P09_AUTH="$API/Modules/Auth/AuthModulu.cs"
P09_MFA="$API/Modules/Auth/MfaModulu.cs"
P09_ADMIN="$API/Modules/Admin/AdminModulu.cs"
p09_ok()  { printf "  ✓ %-46s %s\n" "$1" "$2"; }
p09_kir() { printf "  ✗ %-46s %s\n" "$1" "$2"; HATA=1; }

# ── P09-1) ROTA_YETKI_ESLESMEZ = 0 (Y-39) ────────────────────────────────────
#   TANIM: Pages/ altındaki her `^@page` rotası RotaModulleri.Harita'da BİR
#   satır taşır ve haritadaki her anahtar GERÇEK bir rotadır (İKİ YÖNLÜ).
#   Neden: yetki matrisi (Moduller.Tumu) artık bu haritadan TÜRETİLİR. Yeni bir
#   ekran haritaya yazılmazsa matriste karşılığı olmaz; ekran ya herkese açık
#   kalır (fail-open) ya da sessizce 403 verir. Ölü anahtar da matrisi şişirir.
#   ⚠ Rota şablonundaki tip kısıtı ({Id:int}) soyulur, ad korunur.
#   ÖLÇÜM (2026-09-20 tazesi → rota 42, anahtar 42, eşleşmez 0):
#     grep -rhE '^@page' --include='*.razor' Kuyumcu.Web/Pages \
#       | sed -E 's/^@page[[:space:]]+"([^"]*)".*/\1/' | sed -E 's/\{([A-Za-z]+)(:[a-z]+)?\}/{\1}/g' | sort
#     grep -oE '^\s*\["[^"]+"\]' Kuyumcu.Core/Erisim/RotaModulleri.cs | grep -oE '"[^"]+"' | tr -d '"' | sort
P09_ROTA_L=$(grep -rhE '^@page' --include='*.razor' "$WEB/Pages" 2>/dev/null \
  | sed -E 's/^@page[[:space:]]+"([^"]*)".*/\1/' | sed -E 's/\{([A-Za-z]+)(:[a-z]+)?\}/{\1}/g' | sort -u)
P09_HAR_L=$(grep -oE '^[[:space:]]*\["[^"]+"\]' "$P09_ROTAHARITA" 2>/dev/null \
  | grep -oE '"[^"]+"' | tr -d '"' | sort -u)
P09_ROTA_N=$(printf '%s\n' "$P09_ROTA_L" | grep -c . | tr -d ' ')
P09_ESLESMEZ=$( { comm -3 <(printf '%s\n' "$P09_ROTA_L") <(printf '%s\n' "$P09_HAR_L"); } | grep -c . | tr -d ' ')
if [ "${P09_ROTA_N:-0}" -lt 40 ]; then
  p09_kir "P-09: ROTA_YETKI_ESLESMEZ" "ölçüm BOŞ döndü ($P09_ROTA_N rota) — sayaç oyunlanmış"
elif [ "${P09_ESLESMEZ:-99}" -eq 0 ]; then
  p09_ok "P-09: ROTA_YETKI_ESLESMEZ" "0 (iki yönlü, $P09_ROTA_N rota)"
else
  p09_kir "P-09: ROTA_YETKI_ESLESMEZ" "$P09_ESLESMEZ ≠ 0 — $(comm -3 <(printf '%s\n' "$P09_ROTA_L") <(printf '%s\n' "$P09_HAR_L") | tr -d '\t' | tr '\n' ' ')"
fi

# ── P09-2) OZNITELIKSIZ_UC = 0 (Y-39 · fail-open riski) ──────────────────────
#   TANIM: her [HttpGet/Post/Put/Patch/Delete] eylemi ya METOT ya SINIF
#   düzeyinde bir kapı beyanı taşır: [RolGerekli] · [ModulYetkisi] ·
#   [AllowAnonymous] · [HerkeseAcik("gerekçe")]. Hiçbiri yoksa uç "unutulmuş"
#   sayılır — Mehmet'in fail-open riski tam olarak budur.
#   ⚠ [HerkeseAcik] DAVRANIŞSIZ bir BEYANDIR; gerekçesiz kurulamaz (ctor atar).
#   ⚠ VAKUM KORUMASI: tespit edilen uç sayısı 250'nin altına düşerse ayrıştırıcı
#     bozulmuş demektir ve sayaç 0 dönse bile denetim KIRILIR.
#   ÖLÇÜM (2026-09-20 tazesi → tespit 257 uç, özniteliksiz 0):
#     ayrıştırıcı aşağıdaki gömülü awk'tır (öznitelik bloğu imzada sıfırlanır).
P09_UC_AWK='
/^[[:space:]]*\[/            { blok = blok "\n" $0; next }
/^[[:space:]]*(\/\/|\/\/\/|\*)/ { next }
/^[[:space:]]*$/             { next }
{
  if ($0 ~ /^[[:space:]]*(public|internal)[^=]*\(/ && blok ~ /\[Http(Get|Post|Put|Patch|Delete)/) {
    TOPLAM++;
    if (blok !~ /RolGerekli|ModulYetkisi|AllowAnonymous|HerkeseAcik/ &&
        SINIF !~ /RolGerekli|ModulYetkisi|AllowAnonymous|HerkeseAcik/) {
      ad = $0; sub(/^[[:space:]]+/, "", ad); sub(/[[:space:]]*(=>|\{).*$/, "", ad);
      ACIK = ACIK " " FILENAME ":" NR;
    }
  }
  if ($0 ~ /class[[:space:]]+[A-Za-z0-9_]*Controller/) SINIF = blok;
  blok = "";
}
END { print TOPLAM+0 "\t" ACIK }
'
P09_UC_TOPLAM=0; P09_UC_ACIK=""
for f in $(grep -rlE 'class [A-Za-z0-9_]*Controller' --include='*.cs' "$API" 2>/dev/null); do
  satir=$(awk "$P09_UC_AWK" "$f")
  P09_UC_TOPLAM=$((P09_UC_TOPLAM + $(printf '%s' "$satir" | cut -f1)))
  P09_UC_ACIK="$P09_UC_ACIK$(printf '%s' "$satir" | cut -f2)"
done
P09_UC_ACIK_N=$(printf '%s' "$P09_UC_ACIK" | tr ' ' '\n' | grep -c . | tr -d ' ')
if [ "$P09_UC_TOPLAM" -lt 250 ]; then
  p09_kir "P-09: OZNITELIKSIZ_UC" "ayrıştırıcı yalnız $P09_UC_TOPLAM uç gördü (<250) — ölçüm geçersiz"
elif [ "${P09_UC_ACIK_N:-99}" -eq 0 ]; then
  p09_ok "P-09: OZNITELIKSIZ_UC" "0 ($P09_UC_TOPLAM ucun tamamı beyanlı)"
else
  p09_kir "P-09: OZNITELIKSIZ_UC" "$P09_UC_ACIK_N ≠ 0 —$P09_UC_ACIK"
fi

# ── P09-3) TEK_SAHIP_KILIT_YOLU = 0 (B-36) ───────────────────────────────────
#   TANIM: bir Kullanıcı satırının `Rol` ya da `Aktif` alanını DEĞİŞTİREN her
#   metot ya SonSahipKapisi'nı çağırır ya da muaf listededir. Muaflar:
#     · Olustur        — yeni satır, mevcut SAHİP sayısını DÜŞÜRMEZ
#     · SahiplikDevret — kapının KENDİSİ; tek SaveChanges'te sahip sayısı sabit
#   Neden: kapı bir metottan unutulursa son SAHİP pasife alınıp kiracı kilitlenir
#   (B-36 "tek SAHİP çıkmazı") ve geri dönüş yolu KALMAZ.
#   ÖLÇÜM (2026-09-20 tazesi → yazan metot 4, kapısız 0):
#     grep -nE '(hedef|ben|kullanici)\.(Rol|Aktif)[[:space:]]*=' Kuyumcu.Api/Modules/Kullanici/KullaniciModulu.cs
P09_KILIT_AWK='
function kapat() {
  if (metot != "" && yazar && !kapi && metot !~ /^(Olustur|SahiplikDevret)$/) ACIK = ACIK " " metot;
}
/^[[:space:]]*(\/\/|\*)/ { next }
/^[[:space:]]*(public|private|internal)[[:space:]].*\(/ {
  if (match($0, /[A-Za-z0-9_]+\(/)) {
    kapat();
    metot = substr($0, RSTART, RLENGTH - 1); yazar = 0; kapi = 0;
  }
  next;
}
/(hedef|ben|kullanici)\.(Rol|Aktif)[[:space:]]*=[^=]/ { yazar = 1; if (metot != "") YAZAN[metot] = 1 }
/SonSahipKapisi\(/ { kapi = 1 }
END {
  kapat();
  n = 0; for (m in YAZAN) n++;
  print n "\t" ACIK;
}
'
P09_KILIT=$(awk "$P09_KILIT_AWK" "$P09_KUL")
P09_KILIT_YAZAN=$(printf '%s' "$P09_KILIT" | cut -f1)
P09_KILIT_ACIK=$(printf '%s' "$P09_KILIT" | cut -f2)
P09_KILIT_ACIK_N=$(printf '%s' "$P09_KILIT_ACIK" | tr ' ' '\n' | grep -c . | tr -d ' ')
if [ "${P09_KILIT_YAZAN:-0}" -lt 3 ]; then
  p09_kir "P-09: TEK_SAHIP_KILIT_YOLU" "ayrıştırıcı yalnız $P09_KILIT_YAZAN yazan metot gördü — ölçüm geçersiz"
elif ! grep -q 'private async Task SonSahipKapisi' "$P09_KUL"; then
  p09_kir "P-09: TEK_SAHIP_KILIT_YOLU" "SonSahipKapisi kaldırılmış — B-36 kapısı yok"
elif ! grep -q 'Sahipliği devret' "$P09_KUL"; then
  p09_kir "P-09: TEK_SAHIP_KILIT_YOLU" "kapı mesajı DOĞRU ÇIKIŞ YOLUNU (devir) söylemiyor"
elif [ "${P09_KILIT_ACIK_N:-99}" -eq 0 ]; then
  p09_ok "P-09: TEK_SAHIP_KILIT_YOLU" "0 ($P09_KILIT_YAZAN yazan metodun hepsi kapıdan geçiyor)"
else
  p09_kir "P-09: TEK_SAHIP_KILIT_YOLU" "$P09_KILIT_ACIK_N ≠ 0 —$P09_KILIT_ACIK"
fi

# ── P09-4) YETKISIZ_PLAN_YAZAN_UC = 0 (K3 dilimi · BK-13 / BK-35) ────────────
#   TANIM: `tenant.Plan` yazan HER nokta (a) yetki kapısı arkasında,
#   (b) katalogla SERVİSTE doğrulanmış, (c) denetim izine yazan olmalıdır.
#   Eksik olan her nokta bir GÜVENLİK AÇIĞIDIR (ticari özellik değil).
#   ⚠ DTO DataAnnotations yalnız controller yolunda çalışır — kapı SERVİSTE.
#
#   ⚠ PAKET P-27 UYARLAMASI (2026-09-22): yazan nokta AdminModulu.cs'den
#     Modules/Abonelik/AbonelikServisi.cs'e TAŞINDI (F-1 abonelik dönemi +
#     F-3 plan kapısı). Sayaç kaldırılmadı, HEDEFİ güncellendi; kapı şartları
#     da sertleşti: artık "kapalı liste" değil KATALOG doğrulaması ve
#     YÜKSELTME YASAĞI aranır.
#   ÖLÇÜM (TAZE 2026-09-22 → yazan nokta 1 (AbonelikServisi.cs), yetkisiz 0):
#     grep -rnE '\.Plan[[:space:]]*=[[:space:]]*[^=]' --include='*.cs' Kuyumcu.Api
P09_ABONELIK="$API/Modules/Abonelik/AbonelikServisi.cs"
P09_PLAN_YAZAN=$(grep -rnE '\.Plan[[:space:]]*=[[:space:]]*[^=]' --include='*.cs' "$API" 2>/dev/null \
  | grep -vE ':[0-9]+:[[:space:]]*(//|///|\*)' | wc -l | tr -d ' ')
P09_PLAN_DISI=$(grep -rnE '\.Plan[[:space:]]*=[[:space:]]*[^=]' --include='*.cs' "$API" 2>/dev/null \
  | grep -vE ':[0-9]+:[[:space:]]*(//|///|\*)' | grep -vc 'Modules/Abonelik/AbonelikServisi.cs' | tr -d ' ')
P09_PLAN_KAPI=0
# (a) plan katalogdan doğrulanır (kodda ikinci liste yok)
grep -q 'planlar.FirstOrDefault(p => p.Kod == kod)' "$P09_ABONELIK" || P09_PLAN_KAPI=1
# (b) YÜKSELTME kiracıya kapalı: seviye karşılaştırması + 403
grep -q 'yeni.Seviye > mevcut.Seviye' "$P09_ABONELIK"               || P09_PLAN_KAPI=1
grep -q 'throw new ForbiddenException(YukseltmeRedMesaji)' "$P09_ABONELIK" || P09_PLAN_KAPI=1
# (c) iz: hem başarılı değişim hem REDDEDİLEN girişim yazılır
grep -q 'IzDamgasi}:' "$P09_ABONELIK"                                || P09_PLAN_KAPI=1
grep -q 'RedDamgasi}:' "$P09_ABONELIK"                               || P09_PLAN_KAPI=1
#   ⚠ Rol kapısı: kiracı ucu SAHİP'tedir; SAHİP DIŞINDA bir rol eklenirse
#     kapı GENİŞLEMİŞ sayılır.
P09_ADMIN_ROL=$(grep -nE '^[[:space:]]*\[RolGerekli\(' "$P09_ADMIN" "$API/Modules/Abonelik/AbonelikModulu.cs" | grep -v '///' || true)
[ "$(printf '%s\n' "$P09_ADMIN_ROL" | grep -c 'RolGerekli("SAHIP")')" -ge 2 ] || P09_PLAN_KAPI=1
if printf '%s\n' "$P09_ADMIN_ROL" | grep -qE 'RolGerekli\([^)]*(MUDUR|MUHASEBE|TEZGAHTAR)'; then P09_PLAN_KAPI=1; fi
P09_PLAN_TOPLAM=$((P09_PLAN_DISI + P09_PLAN_KAPI))
if [ "${P09_PLAN_YAZAN:-0}" -lt 1 ]; then
  p09_kir "P-09: YETKISIZ_PLAN_YAZAN_UC" "plan yazan nokta HİÇ bulunamadı — ölçüm geçersiz"
elif [ "$P09_PLAN_TOPLAM" -eq 0 ]; then
  p09_ok "P-09: YETKISIZ_PLAN_YAZAN_UC" "0 ($P09_PLAN_YAZAN yazan nokta: SAHİP + liste + iz)"
else
  p09_kir "P-09: YETKISIZ_PLAN_YAZAN_UC" "$P09_PLAN_TOPLAM ≠ 0 (AdminModulu dışı $P09_PLAN_DISI · kapı eksiği $P09_PLAN_KAPI)"
fi

# ── P09-5) KIMLIK_IFSA_YOLU = 0 (B-35) + MFA BÜTÜNLÜĞÜ (B-6 · B-20) ──────────
#   TANIM: giriş ve /auth/ben yolları kullanıcı VARLIĞINI ifşa etmez — ikisi de
#   TEK generic 401 (AuthServisi.GirisHatasi) döner; zamanlama yan kanalı da
#   sahte hash ile eşitlenir. MFA tarafında: kurtarma kodu YALNIZ başarılı
#   doğrulamada tüketilir, `mfa-kurtarma-kullanildi` izi yazılır ve yarım kalan
#   kurulum sıfırlanabilir (kilit yaratmaz).
#   ÖLÇÜM: aşağıdaki bütünlük kontrolleri (boşaltılırsa denetim kırılır).
P09_KIMLIK=0
grep -q 'GirisHatasi = "E-posta veya şifre hatalı"' "$P09_AUTH"  || P09_KIMLIK=1
grep -q 'SahteHash' "$P09_AUTH"                                   || P09_KIMLIK=1
# Giriş/Ben yollarında NotFound KALMAMALI (eski 404 "Kullanıcı bulunamadı").
if grep -nE 'NotFoundException\("Kullanıcı bulunamadı"\)' "$P09_AUTH" | grep -q .; then P09_KIMLIK=1; fi
grep -q 'mfa-kurtarma-kullanildi' "$P09_MFA"                      || P09_KIMLIK=1
grep -q 'mfa-kurulum-sifirlandi' "$P09_MFA"                       || P09_KIMLIK=1
if [ "$P09_KIMLIK" -eq 0 ]; then
  p09_ok "P-09: KIMLIK_IFSA_YOLU + MFA BÜTÜNLÜĞÜ" "tek 401 + sahte hash + kurtarma izi + sıfırlama"
else
  p09_kir "P-09: KIMLIK_IFSA_YOLU + MFA BÜTÜNLÜĞÜ" "B-35/B-6/B-20 kapıları BOŞALTILMIŞ"
fi

# ── P09-6) GOC_KAPISIZ_TOHUMLAMA = 0 (hasım turu KRİTİK bulgusu) ────────────
#   TANIM: P-09 bazı kapıları YENİDEN ADLANDIRDI (pos←kasa · vezne←islem ·
#   sayim←urun · fiyat-listesi←urun). Eski kiracıda bu anahtarların markeri
#   yoktur; tohumlama KOŞULSUZ çalışırsa sahibin BİLEREK KALDIRDIĞI erişim yeni
#   ad altında sessizce geri açılır (para uçları: POS, vezne). Bu yüzden
#   Varsayilan satırını yazan TEK döngü `GocKapisi` süzgecinden geçmelidir ve
#   `Moduller.AnahtarGocu` haritası BOŞALTILAMAZ.
#   ÖLÇÜM (TAZE, 2026-09-20):
#     grep -c 'if (modeller.Contains(modul) && GocKapisi(rol, modul))' \
#          Kuyumcu.Api/Modules/Yetki/YetkiModulu.cs                      → 1
#     grep -c 'if (modeller.Contains(modul))' …                          → 0
#     AnahtarGocu satır sayısı                                           → 4
#   ⚠ TAVAN ARTMAZ: harita satırı 4'ün ALTINA düşerse (kapı boşaltılmış) kırılır;
#     üstüne çıkmak serbesttir (yeni bir yeniden adlandırma eklenmiş demektir).
P09_YETKI="Kuyumcu.Api/Modules/Yetki/YetkiModulu.cs"
P09_GOC_KAPILI=$(grep -c 'modeller\.Contains(modul) && GocKapisi(rol, modul)' "$P09_YETKI" || true)
P09_GOC_ACIK=$(grep -cE '^\s*if \(modeller\.Contains\(modul\)\)\s*$' "$P09_YETKI" || true)
P09_GOC_HARITA=$(sed -n '/AnahtarGocu = new()/,/^    };$/p' "$P09_YETKI" | grep -cE '^\s*\["[a-z-]+"\]\s*=\s*\[' || true)
if [ "$P09_GOC_HARITA" -lt 4 ]; then
  p09_kir "P-09: GOC_KAPISIZ_TOHUMLAMA" "AnahtarGocu haritası $P09_GOC_HARITA satır (<4) — göç kapısı boşaltılmış"
elif [ "$P09_GOC_KAPILI" -ne 1 ] || [ "$P09_GOC_ACIK" -ne 0 ]; then
  p09_kir "P-09: GOC_KAPISIZ_TOHUMLAMA" "tohumlama döngüsü GocKapisi'sız (kapılı=$P09_GOC_KAPILI açık=$P09_GOC_ACIK) — sessiz yetki genişlemesi"
else
  p09_ok "P-09: GOC_KAPISIZ_TOHUMLAMA" "0 ($P09_GOC_HARITA göçmen anahtarın hepsi kapıdan geçiyor)"
fi

# <<< P-09

# >>> P-10 — SUNUCUNUN KABUL ETTİĞİ ALANLAR FORMA BAĞLI MI + ÜRÜN GÖRSELİ
#     (cırcır; PLAN § 2 P-10, kimlikler Y-3 · Y-8, ilke § 1 md.1 "mekanizma önce")
#
# NEDEN: bir uç yeni alan kabul etmeye başladığında ekranın onu göndermemesi
# DERLEME HATASI VERMEZ ve testten de kaçar — alan sessizce hiç doldurulmaz.
# Y-3'ün kökeni tam olarak budur: yedi alan (ürün RFID · KMTS · görsel URL ·
# açıklama · parite · cari notlar · POS cihazı/taksit) kolonda, DTO'da ve uçta
# HAZIRDI, hiçbir form göndermiyordu. Sayaç bu boşluğu mekanik hâle getirir.
#
# ÖLÇÜM KOMUTU (BK-07 — taban belgeden KOPYALANMADI, burada taze koşuyor):
#   her (DTO, ekran) çifti için:
#     awk ile sınıf gövdesi kesilir -> `[JsonIgnore]` satırları ELENİR
#     (...Verildi/...Gonderildi bayrakları istemci alanı değildir, üç durumlu
#      PATCH sözleşmesinin iç bayrağıdır) -> kalan her `public X Ad { get;`
#     camelCase'e çevrilir ve ekranın gövdesinde `ad = ` olarak aranır.
#   2026-09-21 taze ölçüm: 5 DTO / 0 eşleşmeyen alan -> beyaz liste BOŞ.
#
# TAVAN YALNIZ AZALIR: beyaz liste boştur; doldurulması gerekçe + kurul kaydı ister.
P10_ALANLAR() {   # $1 kaynak dosya, $2 sınıf adı
  awk -v s="class $2" 'index($0,s){f=1;next} f&&/^}/{exit} f' "$1" \
  | grep -v JsonIgnore \
  | grep -oE "public [^;]* [A-Za-z0-9_]+ [{] get;" \
  | awk '{print $(NF-2)}' | sort -u
}
P10_ESLESMEZ=0
P10_ESLESMEZ_LISTE=""
P10_CIFT_SAYISI=0
P10_KONTROL() {   # $1 kaynak, $2 sınıf, $3 razor
  P10_CIFT_SAYISI=$((P10_CIFT_SAYISI + 1))
  local a c
  for a in $(P10_ALANLAR "$1" "$2"); do
    c="$(printf '%s' "${a:0:1}" | tr 'A-Z' 'a-z')${a:1}"
    if ! grep -qE "[^A-Za-z]$c = " "$3"; then
      P10_ESLESMEZ=$((P10_ESLESMEZ + 1))
      P10_ESLESMEZ_LISTE="$P10_ESLESMEZ_LISTE $2.$a"
    fi
  done
}
P10_KONTROL "$API/Modules/Urun/UrunModulu.cs"   UrunOlusturDto  "$WEB/Pages/Urunler.razor"
P10_KONTROL "$API/Modules/Urun/UrunModulu.cs"   UrunGuncelleDto "$WEB/Pages/Urunler.razor"
P10_KONTROL "$API/Modules/Cari/CariModulu.cs"   CariOlusturDto  "$WEB/Pages/Cariler.razor"
P10_KONTROL "$API/Modules/Cari/CariModulu.cs"   CariGuncelleDto "$WEB/Pages/Cariler.razor"
P10_KONTROL "$API/Modules/Satis/OdemeSepeti.cs" OdemeBacagiDto  "$WEB/Pages/Islemler.razor"

if [ "$P10_CIFT_SAYISI" -ne 5 ]; then
  printf "  x %-46s %s cift olculdu (5 olmali - sayac kapsami daraltilamaz)\n" \
    "P-10: DTO_FORM_ESLESMEZ kapsami" "$P10_CIFT_SAYISI"; HATA=1
elif [ "$P10_ESLESMEZ" -eq 0 ]; then
  printf "  ✓ %-46s %s (-eq 0, %s çift)\n" \
    "P-10: DTO_FORM_ESLESMEZ" "$P10_ESLESMEZ" "$P10_CIFT_SAYISI"
else
  printf "  ✗ %-46s %s (-eq 0 olmalı — sunucu alanı formda YOK)\n" \
    "P-10: DTO_FORM_ESLESMEZ" "$P10_ESLESMEZ"
  printf "      →%s\n" "$P10_ESLESMEZ_LISTE"; HATA=1
fi

# ── BU_PAKETTE_EKLENEN_RECORD (cırcır; PLAN § 1 md.9) ───────────────────────
# Razor (Pages + Bilesenler + Layout) içindeki istemci record tanımı sayısı
# P-10 ÖNCESİ taze ölçüldü: 3. P-10 hiç record eklemedi (UrunGorselGalerisi
# `BelgeSatiri`yi kullanır; UrunTamKart/UrunVeri/CariDetayVeri GENİŞLETİLDİ).
# ÖLÇÜM KOMUTU:
#   grep -rhoE '^[[:space:]]*(public|internal|private|sealed)[A-Za-z ]*record [A-Za-z0-9_]+' \
#     --include='*.razor' Kuyumcu.Web/Pages Kuyumcu.Web/Bilesenler Kuyumcu.Web/Layout | grep -c .
P10_RAZOR_RECORD_TAVAN=3
P10_RAZOR_RECORD=$(grep -rhoE '^[[:space:]]*(public|internal|private|sealed)[A-Za-z ]*record [A-Za-z0-9_]+' \
                     --include='*.razor' "$WEB/Pages" "$WEB/Bilesenler" "$WEB/Layout" 2>/dev/null | grep -c .)
if [ "$P10_RAZOR_RECORD" -le "$P10_RAZOR_RECORD_TAVAN" ]; then
  printf "  ✓ %-46s %s (tavan %s — cırcır YALNIZ azalır)\n" \
    "P-10: BU_PAKETTE_EKLENEN_RECORD" "$((P10_RAZOR_RECORD - P10_RAZOR_RECORD_TAVAN))" "$P10_RAZOR_RECORD_TAVAN"
else
  printf "  ✗ %-46s +%s (razor record %s > tavan %s)\n" \
    "P-10: BU_PAKETTE_EKLENEN_RECORD" "$((P10_RAZOR_RECORD - P10_RAZOR_RECORD_TAVAN))" \
    "$P10_RAZOR_RECORD" "$P10_RAZOR_RECORD_TAVAN"; HATA=1
fi

# ── Y-8 mekanizması: galeri bileşeni VAR ve ürün ekranına BAĞLI mı? ─────────
# "Uçta var, ekranda yok" (UX-S25) bir daha olmasın diye bağ MAKİNEYLE aranır.
if [ -f "$WEB/Bilesenler/UrunGorselGalerisi.razor" ] \
   && grep -q "UrunGorselGalerisi" "$WEB/Pages/Urunler.razor"; then
  printf "  ✓ %-46s bileşen + ürün ekranı bağı\n" "P-10: URUN_GORSEL_GALERISI"
else
  printf "  ✗ %-46s galeri bileşeni yok ya da ekrana bağlı değil\n" "P-10: URUN_GORSEL_GALERISI"; HATA=1
fi

# ── K-21 beyanı: RFID alanı donanım İMA EDER, ekranda yazılı olmak ZORUNDA ──
# ⚠ P-36c/3: #u-rfid-ipucu artık ekran dosyasında DÜZ METİN değil; ortak girdi
#   kabuğu (AlanKabugu) onu <ElemanId>-ipucu olarak TÜRETİR. Kaynak denetimi bu
#   yüzden "u-rfid alanı RfidIpucu taşıyor mu" diye sorar — DOM kimliği aynıdır
#   ve UrunEkranTestleri onu #u-rfid-ipucu ile bulmaya devam eder.
# KESIF-P8 (karar 2: görünür metinde iç kalem kodu 0): beyan artık "K-21" kodunu değil, düz
#   cümleyi taşır — "RFID okuyucu bağlantısı yoktur". K-21 kodu yalnız Sozlukler.cs yorumunda.
if grep -q "RfidIpucu = \"[^\"]*RFID okuyucu bağlantısı yoktur" Kuyumcu.Core/I18n/Sozlukler.cs \
   && ! grep -q "RfidIpucu = \"[^\"]*K-21" Kuyumcu.Core/I18n/Sozlukler.cs \
   && perl -0777 -ne 'exit(!(/ElemanId="u-rfid".*?Ipucu="\@Dil\.T\.Urun\.RfidIpucu"/s))' -- "$WEB/Pages/Urunler.razor"; then
  printf "  ✓ %-46s ekranda yazılı\n" "P-10: K21_DONANIM_BEYANI"
else
  printf "  ✗ %-46s RFID alanı var ama 'RFID okuyucu bağlantısı yoktur' beyanı yok (ya da ham K-21 kodu görünür)\n" "P-10: K21_DONANIM_BEYANI"; HATA=1
fi

# <<< P-10

# >>> P-11 — DIŞ KAYNAK DAYANIKLILIĞI: KUR + BİLDİRİM
#     (cırcır; PLAN § 2 P-11, kimlikler B-14 · B-15 · B-16 · B-22 · B-23 · B-24)
#
# NEDEN: dış kaynak (kur servisi, zamanlanmış iş) düştüğünde ekran hatayı
# "kayıt yok"a çevirip son bilinen veriyi SİLİYORDU (UX-S28 / Anayasa md.1
# ihlali). İnterneti kesilen kuyumcu, ekranda duran kuru da kaybediyordu.
# Bu kusur derleme hatası vermez ve mevcut testlerden kaçar — sayaç şart.
#
# ÖLÇÜM KOMUTU (BK-07 — taban BELGEDEN KOPYALANMADI, burada taze koşuyor):
#   perl -0777 -ne 'while(/catch\s*\([^)]*\)[^{;]*\{(.*?)\}/gs){$b=$1;
#     while($b=~/(\w+)\s*=\s*(\[\]|null)\s*;/g){print "x\n"}}' \
#     --include='*.razor' Pages Bilesenler Layout | wc -l
#   2026-09-21 taze ölçüm: P-11 sayfaları 0 · depo geneli 47 (P-11 öncesi 50).
P11_DUSUREN() {   # $@ dosyalar
  perl -0777 -ne 'while(/catch\s*\([^)]*\)[^{;]*\{(.*?)\}/gs){$b=$1;
      while($b=~/(\w+)\s*=\s*(\[\]|null)\s*;/g){print "x\n"}}' "$@" 2>/dev/null | grep -c . || true
}

# (a) P-11'in SAHİPLENDİĞİ ekranlarda mutlak sıfır (plan: "= 0")
P11_SAHIP=$(P11_DUSUREN "$WEB/Pages/Kur.razor" "$WEB/Pages/Bildirimler.razor")
if [ "$P11_SAHIP" -eq 0 ]; then
  printf "  ✓ %-46s %s (-eq 0: /kur + /bildirimler)\n" \
    "P-11: DIS_HATADA_VERI_DUSUREN_YOL" "$P11_SAHIP"
else
  printf "  ✗ %-46s %s (-eq 0 olmalı — catch içinde veri alanı boşaltılmış)\n" \
    "P-11: DIS_HATADA_VERI_DUSUREN_YOL" "$P11_SAHIP"; HATA=1
fi

# (b) DEPO GENELİ CIRCIR: taban 47, YALNIZ AZALIR.
#     ⚠ SAPMA (rapora yazıldı): plan "= 0" diyor; depo genelinde bugün 47 var
#     ve bunların tamamı P-11'in kapsamı DIŞINDAKİ ekranlarda (kapsam kilidi).
#     Tavan ARTIRILMAZ; sonraki paketler düşürür.
P11_DUSUREN_TAVAN=47
P11_DUSUREN_TOPLAM=$(P11_DUSUREN "$WEB"/Pages/*.razor "$WEB"/Bilesenler/*.razor "$WEB"/Layout/*.razor)
if [ "$P11_DUSUREN_TOPLAM" -le "$P11_DUSUREN_TAVAN" ]; then
  printf "  ✓ %-46s %s (tavan %s — cırcır YALNIZ azalır)\n" \
    "P-11: DIS_HATADA_VERI_DUSUREN_YOL_TOPLAM" "$P11_DUSUREN_TOPLAM" "$P11_DUSUREN_TAVAN"
else
  printf "  ✗ %-46s %s > tavan %s\n" \
    "P-11: DIS_HATADA_VERI_DUSUREN_YOL_TOPLAM" "$P11_DUSUREN_TOPLAM" "$P11_DUSUREN_TAVAN"; HATA=1
fi

# ── SATIR_ICI_STYLE (cırcır; AZALAN) ────────────────────────────────────────
# ÖLÇÜM KOMUTU:
#   grep -rhoE 'style="' --include='*.razor' Pages Bilesenler Layout | grep -c .
#   2026-09-21 taze ölçüm: 155 (P-11 öncesi 156 — Kur.razor'daki tek satır içi
#   style `.hucre-tek-satir` sınıfına taşındı). Bildirimler.razor zaten 0'dı.
# ⬇ P-16 (2026-09-21) TAZE ÖLÇÜM: 154. Tavan 155 idi ama depoda 154 vardı;
#   cırcır YALNIZ azalır — P-16 iki satır içi style EKLEMEDEN (.satir--alt-hizali
#   ve .alan--dar sınıflarıyla) çalıştı ve tavanı gerçeğe indirdi.
# ⬇ P-18 (2026-09-21) TAZE ÖLÇÜM: 143. P-18 on bir satır içi style'ı sınıfa
#   taşıdı (PastaGrafik lejantı 3 · Raporlar yaşlandırma 4 · Islemler 2 ·
#   Cariler 1 · MasakFormu 1). Cırcır YALNIZ azalır; tavan gerçeğe indirildi.
P11_STYLE_TAVAN=143
P11_STYLE=$(grep -rhoE 'style="' --include='*.razor' \
              "$WEB/Pages" "$WEB/Bilesenler" "$WEB/Layout" 2>/dev/null | grep -c . || true)
if [ "$P11_STYLE" -le "$P11_STYLE_TAVAN" ]; then
  printf "  ✓ %-46s %s (tavan %s — cırcır YALNIZ azalır)\n" \
    "P-11: SATIR_ICI_STYLE" "$P11_STYLE" "$P11_STYLE_TAVAN"
else
  printf "  ✗ %-46s %s > tavan %s\n" "P-11: SATIR_ICI_STYLE" "$P11_STYLE" "$P11_STYLE_TAVAN"; HATA=1
fi

# P-11'in sahiplendiği iki ekranda satır içi style MUTLAK SIFIR.
P11_STYLE_SAHIP=$(grep -hoE 'style="' "$WEB/Pages/Kur.razor" "$WEB/Pages/Bildirimler.razor" 2>/dev/null | grep -c . || true)
if [ "$P11_STYLE_SAHIP" -eq 0 ]; then
  printf "  ✓ %-46s %s (-eq 0: /kur + /bildirimler)\n" "P-11: SATIR_ICI_STYLE_P11" "$P11_STYLE_SAHIP"
else
  printf "  ✗ %-46s %s (-eq 0 olmalı)\n" "P-11: SATIR_ICI_STYLE_P11" "$P11_STYLE_SAHIP"; HATA=1
fi

# ── B-22 mekanizması: /bildirimler BASİT MOD menüsünde mi? ──────────────────
# "Rozet var, gidilecek bağlantı yok" bir daha olmasın diye bağ MAKİNEYLE aranır.
if grep -q '"/bildirimler"' "$WEB/Servisler/BasitMenu.cs"; then
  printf "  ✓ %-46s BasitMenu.Yollar içinde\n" "P-11: BILDIRIM_BASIT_MENUDE"
else
  printf "  ✗ %-46s /bildirimler basit modda menüde YOK (B-22)\n" "P-11: BILDIRIM_BASIT_MENUDE"; HATA=1
fi

# ── B-24 mekanizması: iş ayarı ucu VAR ve EKRANA BAĞLI mı? ──────────────────
# UX-S25 "sunucuda uç var, ekranda düğme yok" kabul edilmez.
if grep -q 'HttpPatch("ayarlar")' "$API/Modules/Isler/IslerModulu.cs" \
   && grep -q 'is-ayar-kaydet' "$WEB/Pages/Bildirimler.razor" \
   && grep -q '/isler/ayarlar' "$WEB/Pages/Bildirimler.razor"; then
  printf "  ✓ %-46s PATCH /isler/ayarlar + ekran bağı\n" "P-11: IS_AYARI_DUZENLENEBILIR"
else
  printf "  ✗ %-46s iş ayarı ucu ya da ekran bağı YOK (B-24)\n" "P-11: IS_AYARI_DUZENLENEBILIR"; HATA=1
fi

# ── B-24 fail-closed: kiracı ayarı yapılandırma kapısını GEVŞETEMEZ ─────────
if grep -q 'if (!Etkin(i)) return false;' "$API/Modules/Isler/IsCalistirici.cs"; then
  printf "  ✓ %-46s ayar yalnız daraltır\n" "P-11: IS_AYARI_FAIL_CLOSED"
else
  printf "  ✗ %-46s kiracı ayarı yapılandırma kapısını gevşetebiliyor\n" "P-11: IS_AYARI_FAIL_CLOSED"; HATA=1
fi

# <<< P-11

# >>> P-12 — DEFTER TARAFI DÜZENLENEBİLİRLİK + STORNO GÖRÜNÜRLÜĞÜ
#     (cırcır; PLAN § 2 P-12, kimlikler B-19 · B-38 · B-41 · B-42 · B-43)
#
# NEDEN: "açıklama düzeltilebilsin" isteği, bir sonraki oturumda sessizce
# "tutar da düzeltilsin"e dönüşür — ve o an defter yerinde değişmeye başlar
# (ADR-0004 ihlali, storno motoru baypas edilir). Bu iki sayaç o kaymayı
# mekanik olarak engeller; derleme hatası vermeyen, testten kaçan kusurdur.
#
# ── SAYAÇ 1: STORNO_DISI_FINANSAL_PATCH = 0 ────────────────────────────────
# ÖLÇÜM KOMUTU (BK-07 — taban BELGEDEN KOPYALANMADI, burada taze koşuyor):
#   her düzeltme DTO'sunun gövdesi kesilir, [JsonIgnore] bayrakları elenir,
#   kalan alan adları finansal ad listesiyle karşılaştırılır.
#   2026-09-21 taze ölçüm: 3 DTO / 0 finansal alan.
# TAVAN ARTIRILMAZ: bu sayaç MUTLAK SIFIRDIR (plan "zorunlu" diyor).
P12_DTO_ALAN() {   # $1 kaynak dosya, $2 sınıf adı
  awk -v s="class $2" 'index($0,s){f=1;next} f&&/^}/{exit} f' "$1" \
  | grep -v JsonIgnore \
  | grep -oE "public [^;]* [A-Za-z0-9_]+ [{] get;" \
  | awk '{print $(NF-2)}' | sort -u
}
# DİKKAT: betik BASH; ${=VAR} zsh sözdizimidir -> burada DİZİ kullanılır.
P12_FINANSAL=( Tutar Yon OdemeTuru KomisyonOran ParaBirimi HasGrami AyarId GramAgirlik Maden Bakiye KasaId BankaHesapId CariId )
P12_FIN_IHLAL=0
P12_FIN_LISTE=""
P12_DTO_KONTROL() {   # $1 kaynak, $2 sınıf
  local a y
  for a in $(P12_DTO_ALAN "$1" "$2"); do
    for y in "${P12_FINANSAL[@]}"; do
      [ "$a" = "$y" ] && { P12_FIN_IHLAL=$((P12_FIN_IHLAL + 1)); P12_FIN_LISTE="$P12_FIN_LISTE $2.$a"; }
    done
  done
}
P12_DTO_KONTROL "$API/Modules/Kasa/KasaModulu.cs"     KasaHareketDuzeltDto
P12_DTO_KONTROL "$API/Modules/Banka/BankaModulu.cs"   HareketDuzeltDto
P12_DTO_KONTROL "$API/Modules/Emanet/EmanetModulu.cs" EmanetHareketDuzeltDto
if [ "$P12_FIN_IHLAL" -eq 0 ]; then
  printf "  ✓ %-46s %s (-eq 0: 3 düzeltme DTO'su)\n" \
    "P-12: STORNO_DISI_FINANSAL_PATCH" "$P12_FIN_IHLAL"
else
  printf "  ✗ %-46s %s (-eq 0 olmalı —%s)\n" \
    "P-12: STORNO_DISI_FINANSAL_PATCH" "$P12_FIN_IHLAL" "$P12_FIN_LISTE"; HATA=1
fi

# ── SAYAÇ 2: STORNO_GORUNMEYEN_EKRAN = 0 ───────────────────────────────────
# "Sunucuda uç var, ekranda düğme yok" kabul edilmez (UX-S25). Defter/para
# satırı gösteren dört ekranın HER BİRİNDE geri alma yolu ARANIR.
# ÖLÇÜM KOMUTU:
#   grep -c '<önek>-storno-' <ekran>   (vezne'de gerekçeli iptal: 'vz-iptal-')
#   2026-09-21 taze ölçüm: 4/4 ekranda yol VAR -> 0 eksik.
P12_STORNO_EKSIK=0
P12_STORNO_LISTE=""
P12_EKRAN_KONTROL() {   # $1 razor, $2 aranan id öneki
  if ! grep -q -- "$2" "$1"; then
    P12_STORNO_EKSIK=$((P12_STORNO_EKSIK + 1))
    P12_STORNO_LISTE="$P12_STORNO_LISTE $(basename "$1")"
  fi
}
P12_EKRAN_KONTROL "$WEB/Pages/Kasalar.razor" "kh-storno-"
P12_EKRAN_KONTROL "$WEB/Pages/Banka.razor"   "bh-storno-"
P12_EKRAN_KONTROL "$WEB/Pages/Emanet.razor"  "eh-storno-"
P12_EKRAN_KONTROL "$WEB/Pages/Vezne.razor"   "vz-iptal-"
if [ "$P12_STORNO_EKSIK" -eq 0 ]; then
  printf "  ✓ %-46s %s (-eq 0: kasalar·banka·emanet·vezne)\n" \
    "P-12: STORNO_GORUNMEYEN_EKRAN" "$P12_STORNO_EKSIK"
else
  printf "  ✗ %-46s %s (-eq 0 olmalı —%s)\n" \
    "P-12: STORNO_GORUNMEYEN_EKRAN" "$P12_STORNO_EKSIK" "$P12_STORNO_LISTE"; HATA=1
fi

# ── B-42 mekanizması: banka dekontu TEK MOTORDAN geri alınır ───────────────
# İş mantığı BankaModulu'na kopyalanmasın diye bağ MAKİNEYLE aranır.
if grep -q 'BankaHareketiStorno' "$API/Modules/Satis/StornoServisi.cs" \
   && grep -q 'storno.BankaHareketiStorno' "$API/Modules/Banka/BankaModulu.cs"; then
  printf "  ✓ %-46s StornoServisi.BankaHareketiStorno + uç bağı\n" "P-12: BANKA_STORNO_TEK_MOTOR"
else
  printf "  ✗ %-46s banka stornosu tek motordan geçmiyor (B-42)\n" "P-12: BANKA_STORNO_TEK_MOTOR"; HATA=1
fi

# ── B-38 mekanizması: /vezne düzenleme formu OLUŞTURMA formuyla aynı küme ──
# UX-S26: oluşturmada var olan alan düzenlemede eksik kalamaz.
if grep -q 'vz-f-iscilik-' "$WEB/Pages/Vezne.razor" \
   && grep -q 'vz-d-iscilik-' "$WEB/Pages/Vezne.razor"; then
  printf "  ✓ %-46s vz-f-iscilik + vz-d-iscilik\n" "P-12: VEZNE_ISCILIK_DUZENLENEBILIR"
else
  printf "  ✗ %-46s düzenleme çekmecesinde işçilik alanı YOK (B-38)\n" "P-12: VEZNE_ISCILIK_DUZENLENEBILIR"; HATA=1
fi

# ── Plan DoD md.4: düzeltme DENETİM İZİNE eski→yeni değerle yazılır ────────
P12_IZ=0
for f in "$API/Modules/Kasa/KasaModulu.cs" "$API/Modules/Banka/BankaModulu.cs" "$API/Modules/Emanet/EmanetModulu.cs"; do
  grep -q 'Duzeltme"' "$f" && P12_IZ=$((P12_IZ + 1))
done
if [ "$P12_IZ" -eq 3 ]; then
  printf "  ✓ %-46s %s/3 modül düzeltme izi yazıyor\n" "P-12: DUZELTME_DENETIM_IZI" "$P12_IZ"
else
  printf "  ✗ %-46s %s/3 (eksik modül düzeltmeyi izsiz yazıyor)\n" "P-12: DUZELTME_DENETIM_IZI" "$P12_IZ"; HATA=1
fi

# <<< P-12

# >>> P-13 — KART / AYAR / UYUM EKRAN BORÇLARI
#     (cırcır; PLAN § 2 P-13, kimlikler B-39 · B-40 · B-47 · B-48 · B-12 ·
#      B-49 · B-17 · B-27 · B-61)
#
# NEDEN: bu paketin kapattığı iki kusur sınıfı DERLEME HATASI VERMEZ ve
# TESTTEN KAÇAR: (1) bir alanın gerekçesiz kilitlenmesi, (2) iç kodun ekrana
# sızması. İkisi de bir sonraki oturumda sessizce geri gelir; sayaç engeller.
# DİKKAT: betik BASH. ${=VAR} zsh sözdizimidir ve bash'te "bad substitution"
# verip döngüyü HİÇ çalıştırmaz (sayaç sahte 0 döner). Bu yüzden DİZİ kullanılır.
P13_SAYFALAR=(Cariler.razor UrunAgaci.razor Isletme.razor Yedekler.razor Uyum.razor GiderPusulalari.razor Kurulum.razor)
P13_BEKLENEN_SAYFA=7

# ── SAYAÇ 1: GEREKCESIZ_KILITLI_ALAN = 0 ───────────────────────────────────
# KURAL (UX-S30): kilitli her eylem/alan SEBEBİNİ söyler. Geçici durumlar
#   (kaydediliyor / calisiyor / yukleniyor), salt okunur ayna (@(true)) ve
#   rol kapısı (SaltOkunur / Kilitli) sebep istemez; KURAL kaynaklı her kilit
#   ±14 satır içinde bir sebep işareti taşır.
# ÖLÇÜM KOMUTU (BK-07 — taban BELGEDEN KOPYALANMADI, burada taze koşuyor):
#   awk ile her `disabled="` satırı taranır, geçici/rol kalıpları elenir,
#   kalanlarda sebep işareti (-kilit · -hata · KilitSebebi · DegismezIpucu ·
#   title= · Gerekce · aria-describedby · alan-hata · hata-metni) aranır.
#   2026-09-21 taze ölçüm: 7 sayfa / 0 gerekçesiz kilit.
# TAVAN ARTIRILMAZ: bu sayaç MUTLAK SIFIRDIR (plan "= 0" diyor).
P13_KILIT=0
P13_KILIT_LISTE=""
P13_TARANAN=0
for f in "${P13_SAYFALAR[@]}"; do
  d="$WEB/Pages/$f"
  [ -f "$d" ] || { printf "  ✗ %-46s %s YOK\n" "P-13: GEREKCESIZ_KILITLI_ALAN" "$d"; HATA=1; continue; }
  P13_TARANAN=$((P13_TARANAN + 1))
  bulgu=$(awk '{satir[NR]=$0}
    END{ for(n=1;n<=NR;n++){ s=satir[n]
      if(s !~ /disabled="/) continue
      if(s ~ /disabled="@\(?(kaydediliyor|calisiyor|yukleniyor|false)\)?"/) continue
      if(s ~ /disabled="@\(true\)"/) continue
      if(s ~ /SaltOkunur|Kilitli/) continue
      bulundu=0
      for(i=n-4;i<=n+14;i++) if(i in satir && satir[i] ~ /-kilit|-hata|KilitSebebi|DegismezIpucu|Gerekce|aria-describedby|alan-hata|hata-metni|title=/) bulundu=1
      if(!bulundu) print n
    }}' "$d" | wc -l | tr -d ' ')
  if [ "$bulgu" -gt 0 ]; then
    P13_KILIT=$((P13_KILIT + bulgu)); P13_KILIT_LISTE="$P13_KILIT_LISTE $f:$bulgu"
  fi
done
if [ "$P13_TARANAN" -ne "$P13_BEKLENEN_SAYFA" ]; then
  printf "  ✗ %-46s TARAMA YAPILMADI (%s/%s sayfa) — sayac gecersiz\n" \
    "P-13: GEREKCESIZ_KILITLI_ALAN" "$P13_TARANAN" "$P13_BEKLENEN_SAYFA"; HATA=1
elif [ "$P13_KILIT" -eq 0 ]; then
  printf "  ✓ %-46s %s (-eq 0: %s sayfa tarandı)\n" "P-13: GEREKCESIZ_KILITLI_ALAN" "$P13_KILIT" "$P13_TARANAN"
else
  printf "  ✗ %-46s %s (-eq 0 olmalı —%s)\n" \
    "P-13: GEREKCESIZ_KILITLI_ALAN" "$P13_KILIT" "$P13_KILIT_LISTE"; HATA=1
fi

# ── SAYAÇ 2: IC_KOD_SIZINTISI = 0 ──────────────────────────────────────────
# KURAL (UX-S31): ham iç kod kullanıcıya gösterilmez. Üç desen sayılır:
#   (a) `PAKET n` / `paket n` gibi iç paket adı,
#   (b) `FAZ_X` / `FAZ A` gibi iç faz adı,
#   (c) BÜYÜK_HARF_ALT_ÇİZGİLİ kod (DOVIZ_HESAP_FAZ_B) — YALNIZ razor'ın
#       ÇIKTI tarafında; yorum satırları ve C# kodu sayılmaz.
# ÖLÇÜM KOMUTU: razor yorumları (@* ... *@) ve // satırları elenir, kalan
#   metinde `>metin<` ve `@Dil...` dışı çıplak kod desenleri aranır.
#   2026-09-21 taze ölçüm: 7 sayfa / 0 sızıntı (Kurulum.razor'daki
#   `@h.HataKodu` bu pakette `@h.Kimlik` ile değiştirildi).
# TAVAN ARTIRILMAZ: MUTLAK SIFIR.
P13_SIZINTI=0
P13_SIZINTI_LISTE=""
P13_TARANAN2=0
for f in "${P13_SAYFALAR[@]}"; do
  d="$WEB/Pages/$f"
  [ -f "$d" ] || continue
  P13_TARANAN2=$((P13_TARANAN2 + 1))
  bulgu=$(awk '
    /@\*/{yorum=1}
    { s=$0
      if(yorum){ if(s ~ /\*@/) yorum=0; next }
      if(s ~ /^[[:space:]]*(\/\/|@\*)/) next
      # (a) iç paket adı, (b) iç faz adı — ÇIKTI metninde
      if(s ~ />[^<]*(PAKET|Paket) [0-9]/) { print NR; next }
      if(s ~ />[^<]*FAZ[_ ][A-Z]/) { print NR; next }
      # (c) ham kod yazdıran ifade: @...HataKodu / @...Kodu / @...Kod)
      if(s ~ /@[A-Za-z_.]*(HataKodu|IcKod)\b/) { print NR; next }
      # (d) çıktı metninde çıplak BÜYÜK_HARF_ALT_ÇİZGİLİ kod
      if(s ~ />[^<]*[A-Z][A-Z0-9]{2,}_[A-Z0-9_]+/) { print NR; next }
    }' "$d" | wc -l | tr -d ' ')
  if [ "$bulgu" -gt 0 ]; then
    P13_SIZINTI=$((P13_SIZINTI + bulgu)); P13_SIZINTI_LISTE="$P13_SIZINTI_LISTE $f:$bulgu"
  fi
done
if [ "$P13_TARANAN2" -ne "$P13_BEKLENEN_SAYFA" ]; then
  printf "  ✗ %-46s TARAMA YAPILMADI (%s/%s sayfa) — sayac gecersiz\n" \
    "P-13: IC_KOD_SIZINTISI" "$P13_TARANAN2" "$P13_BEKLENEN_SAYFA"; HATA=1
elif [ "$P13_SIZINTI" -eq 0 ]; then
  printf "  ✓ %-46s %s (-eq 0: %s sayfa tarandı)\n" "P-13: IC_KOD_SIZINTISI" "$P13_SIZINTI" "$P13_TARANAN2"
else
  printf "  ✗ %-46s %s (-eq 0 olmalı —%s)\n" \
    "P-13: IC_KOD_SIZINTISI" "$P13_SIZINTI" "$P13_SIZINTI_LISTE"; HATA=1
fi

# ── SAYAÇ 3 (destek): KATEGORI_PASIFE_ALMA_YOLU ────────────────────────────
# "Sunucuda uç var, ekranda düğme yok" yasağının (UX-S25) mekanik kilidi:
# kategori DELETE ucu ve ekran düğmesi BİRLİKTE bulunur.
P13_KAT=0
grep -q 'HttpDelete("{id}")' "$API/Modules/Urun/KategoriModulu.cs" && P13_KAT=$((P13_KAT + 1))
grep -q 'k-pasife-' "$WEB/Pages/UrunAgaci.razor" && P13_KAT=$((P13_KAT + 1))
grep -q 'k-aktife-al-' "$WEB/Pages/UrunAgaci.razor" && P13_KAT=$((P13_KAT + 1))
if [ "$P13_KAT" -eq 3 ]; then
  printf "  ✓ %-46s uç + pasife al + aktife al\n" "P-13: KATEGORI_PASIFE_ALMA_YOLU"
else
  printf "  ✗ %-46s %s/3 (uç ya da ekran yolu eksik — B-40)\n" \
    "P-13: KATEGORI_PASIFE_ALMA_YOLU" "$P13_KAT"; HATA=1
fi

# <<< P-13

# >>> P-14 — MENÜ / BİLGİ MİMARİSİ + ETİKET ÇARPIŞMALARI
#     (cırcır; PLAN § 2 P-14, kimlik Y-9; kaynak EKRAN-ALAN-DOGRULAMA-2026-09-18.md sıra 9)
#
# NEDEN: menü bozulması DERLEME HATASI VERMEZ. Yeni bir sayfa eklenip menüye
# konmazsa sayfa erişilemez kalır; iki bağlantıya birbirine karışan ad verilirse
# kuyumcu yanlış ekrana girer; basit mod listesi menüden kopunca rozet hiçbir
# yere götürmez. Üçü de sessizdir — sayaç engeller.
# DİKKAT: betik BASH; ${=VAR} zsh sözdizimidir, burada DİZİ kullanılır.
P14_MENU="$WEB/Layout/MenuTanimi.cs"
# TEMA TEMİZLİĞİ (17.10): kenar çubuğu (KenarCubugu.razor) kabukla kalktı; menünün TAMAMINI MenuTanimi'nden
# çizen ekran artık "Tüm ekranlar" yüzüdür (YildizEkranlar). Sayaçlar aynı kuralı orada ölçer.
P14_KENAR="$WEB/Bilesenler/Yildiz/YildizEkranlar.razor"
P14_BASIT="$WEB/Servisler/BasitMenu.cs"
# PAKET S1-g: menü adları hub'dan (Sozlukler.cs Nav.*) AYRI sözlüğe taşındı
# (Sozlukler.Menu.cs — MenuAdlari, tuvalle birebir). Sayaç artık oradan okur.
P14_SOZLUK="Kuyumcu.Core/I18n/Sozlukler.Menu.cs"
P14_EKSIK=0
for f in "$P14_MENU" "$P14_KENAR" "$P14_BASIT" "$P14_SOZLUK"; do
  [ -f "$f" ] || { printf "  ✗ %-46s %s YOK\n" "P-14: KAYNAK DOSYA" "$f"; HATA=1; P14_EKSIK=1; }
done

if [ "$P14_EKSIK" = 0 ]; then

# ── SAYAÇ 1: MENU_AYNI_AD = 0 ──────────────────────────────────────────────
# KURAL (P-14 DoD): "aynı metin iki farklı hedefe gitmez". Çarpışma ölçüsü,
#   MenuTanimi.AyirtEdiciAd ile AYNI kuraldır (PAKET S1-g): Türkçe harfler
#   katlanır, çoğul eki (leri/lari/ler/lar) kırpılır. P-14'teki "genel ek
#   sözcükleri at" adımı S1-g'de KALDIRILDI: kurucunun tuvali "Kasalar / Kasa
#   raporu", "Vade takvimi / Vade takibi", "POS cihazları / POS alacakları"
#   adlarını kullanır — ayırt edici sözcük o eklerdir (plan ile tuval
#   çelişirse TUVAL geçerlidir). İki farklı hedef aynı ada iniyorsa çarpışma.
# ÖLÇÜM KOMUTU (BK-07 — taban BELGEDEN KOPYALANMADI, burada taze koşuyor):
#   MenuTanimi.cs'teki (yol, `m => m.<Anahtar>`) çiftleri + Sozlukler.Menu.cs
#   `MenuAdlari.Tr` bloğu
#   awk ile eşlenir, sadeleştirilir, tekrar eden ad sayılır.
#   P-14 ÖNCESİ ölçüm 4 idi (İşlemler↔İşlem Defteri · Kasalar↔Kasa Defteri ·
#   Vade Takvimi↔Vade Takibi · POS Cihazları↔POS Alacakları).
# TAVAN ARTIRILMAZ: bu sayaç MUTLAK SIFIRDIR.
P14_TR_NAV=$(awk '
  /public static readonly MenuAdlariMetinleri Tr = new\(/ { tr=1; next }
  tr && /(\);[ \t]*$|MenuAdlariMetinleri En = new)/ { exit }
  tr && match($0, /^        [A-Za-z]+: new\("/) {
    k=$0; sub(/^ +/,"",k); sub(/:.*/,"",k)
    v=$0; sub(/^[^"]*"/,"",v); sub(/".*$/,"",v)
    print k "\t" v
  }' "$P14_SOZLUK")
P14_CIFT=$(grep -oE '"(/[^"]*)", *(null|"[a-z]+"),.*m => m\.[A-Za-z]+' "$P14_MENU" \
  | sed -E 's/^"([^"]*)".*m => m\.([A-Za-z]+)$/\1\t\2/')
P14_ADLAR=$( { printf '%s\n' "$P14_TR_NAV" | sed 's/^/S\t/'
                printf '%s\n' "$P14_CIFT"   | sed 's/^/C\t/'; } | awk -F'\t' '
  $1=="S" { deger[$2]=$3; next }
  $1=="C" { n++; yolu[n]=$2; anah[n]=$3 }
  END{
    genel="defter rapor takib takip takvim cihaz alacak liste"
    g=split(genel, ge, " ")
    for(i=1;i<=n;i++){
      yol=yolu[i]; anahtar=anah[i]
      if(!(anahtar in deger)) { print "?EKSIK\t" anahtar; continue }
      e=tolower(deger[anahtar])
      gsub(/İ|I|ı/,"i",e); gsub(/Ş|ş/,"s",e); gsub(/Ğ|ğ/,"g",e)
      gsub(/Ü|ü/,"u",e); gsub(/Ö|ö/,"o",e); gsub(/Ç|ç/,"c",e); gsub(/Â|â/,"a",e)
      m=split(e, sz, " "); sade=""; ilk=""
      for(j=1;j<=m;j++){
        w=sz[j]
        if(length(w)>6 && (substr(w,length(w)-3)=="leri" || substr(w,length(w)-3)=="lari")) w=substr(w,1,length(w)-4)
        else if(length(w)>5 && (substr(w,length(w)-2)=="ler" || substr(w,length(w)-2)=="lar")) w=substr(w,1,length(w)-3)
        if(j==1) ilk=w
        # S1-g: genel ek sözcükler ARTIK ATILMAZ (MenuTanimi.AyirtEdiciAd) —
        # tuvalin "Kasalar/Kasa raporu", "Vade takvimi/Vade takibi" çiftlerinde
        # ayırt edici sözcük tam da odur. Kural hâlâ SIFIR (çoğul eki kırpılır).
        sade = (sade=="" ? w : sade " " w)
      }
      if(sade=="") sade=ilk
      print sade "\t" yol
    }
  }')
P14_EKSIK_ANAHTAR=$(printf '%s\n' "$P14_ADLAR" | grep -c '^?EKSIK' || true)
P14_AYNI=$(printf '%s\n' "$P14_ADLAR" | grep -v '^?EKSIK' | sort -u | cut -f1 | sort | uniq -d | wc -l | tr -d ' ')
P14_SAYILAN=$(printf '%s\n' "$P14_ADLAR" | grep -vc '^?EKSIK' || true)
if [ "$P14_EKSIK_ANAHTAR" -ne 0 ]; then
  printf "  ✗ %-46s %s Nav anahtarı sözlükte YOK — sayac gecersiz\n" \
    "P-14: MENU_AYNI_AD" "$P14_EKSIK_ANAHTAR"; HATA=1
elif [ "$P14_SAYILAN" -lt 30 ]; then
  printf "  ✗ %-46s TARAMA YAPILMADI (%s etiket) — sayac gecersiz\n" \
    "P-14: MENU_AYNI_AD" "$P14_SAYILAN"; HATA=1
elif [ "$P14_AYNI" -eq 0 ]; then
  printf "  ✓ %-46s %s (-eq 0: %s etiket sadeleştirildi)\n" "P-14: MENU_AYNI_AD" "$P14_AYNI" "$P14_SAYILAN"
else
  printf "  ✗ %-46s %s (-eq 0 olmalı — %s)\n" "P-14: MENU_AYNI_AD" "$P14_AYNI" \
    "$(printf '%s\n' "$P14_ADLAR" | sort -u | cut -f1 | sort | uniq -d | tr '\n' ' ')"; HATA=1
fi

# ── SAYAÇ 2: MENU_KAPSAM = 0 (erişilemeyen @page) ──────────────────────────
# KURAL: her `@page` rotası ya kenar çubuğundadır, ya alt bloktadır, ya da
#   MenuTanimi.MenusuzRotalar'da GEREKÇESİYLE (hangi dosya oraya götürüyor)
#   yazılıdır. Gerekçesiz rota = ekranda düğmesi olmayan sayfa (UX-S25).
# ÖLÇÜM KOMUTU: Pages/*.razor `^@page` satırları ile MenuTanimi.cs'teki yollar
#   ve muafiyet anahtarları karşılaştırılır (taze ölçüm, belgeden kopya yok).
P14_ROTALAR=$(grep -rhE '^@page' --include='*.razor' "$WEB/Pages" | sed -E 's/^@page +"//; s/"$//' | sed -E 's|(.)/$|\1|' | sort -u)
P14_MENUDE=$(grep -oE 'new\(MenuGrubu\.[A-Za-z]+, "[^"]+"' "$P14_MENU" | sed -E 's/.*"([^"]+)"/\1/' | sort -u)
P14_MUAF=$(awk '/MenusuzRotalar =/,/\};/' "$P14_MENU" | grep -oE '\["[^"]+"\] = "[^"]+"' | sed -E 's/^\["([^"]+)".*/\1/' | sort -u)
P14_ERISILEMEZ=$(comm -23 <(printf '%s\n' "$P14_ROTALAR") <(printf '%s\n%s\n' "$P14_MENUDE" "$P14_MUAF" | sort -u))
P14_ROTA_SAYI=$(printf '%s\n' "$P14_ROTALAR" | grep -c . || true)
P14_KAPSAM=$(printf '%s\n' "$P14_ERISILEMEZ" | grep -c . || true)
if [ "$P14_ROTA_SAYI" -lt 40 ]; then
  printf "  ✗ %-46s TARAMA YAPILMADI (%s rota) — sayac gecersiz\n" \
    "P-14: MENU_KAPSAM" "$P14_ROTA_SAYI"; HATA=1
elif [ "$P14_KAPSAM" -eq 0 ]; then
  printf "  ✓ %-46s %s (-eq 0: %s rota tarandı)\n" "P-14: MENU_KAPSAM" "$P14_KAPSAM" "$P14_ROTA_SAYI"
else
  printf "  ✗ %-46s %s (-eq 0 olmalı — %s)\n" "P-14: MENU_KAPSAM" "$P14_KAPSAM" \
    "$(printf '%s' "$P14_ERISILEMEZ" | tr '\n' ' ')"; HATA=1
fi

# ── SAYAÇ 3: BASIT_MOD_TUTARSIZ = 0 ────────────────────────────────────────
# KURAL (P-14 DoD): rozeti olan her özelliğin sayfası basit mod menüsünde de
#   olmalı; ayrıca basit menüdeki her yol kenar çubuğu tanımında bulunmalı.
#   Aksi hâlde basit modda rozete basan kullanıcı hiçbir yere varamaz.
# ÖLÇÜM KOMUTU: UstBar.razor'daki rozet href'leri + BasitMenu.Yollar dizisi
#   MenuTanimi yollarıyla karşılaştırılır.
P14_BASIT_YOL=$(grep -oE '^\s+"(/[^"]*)",' "$P14_BASIT" | sed -E 's/.*"([^"]*)".*/\1/' | sort -u)
P14_BASIT_SAYI=$(printf '%s\n' "$P14_BASIT_YOL" | grep -c . || true)
P14_ROZET=$(grep -oE 'href="(/[^"]*)"' "$WEB/Layout/UstBar.razor" | sed -E 's/href="([^"]*)"/\1/' | sort -u)
P14_TUTARSIZ=0
P14_TUTARSIZ_LISTE=""
while IFS= read -r y; do
  [ -n "$y" ] || continue
  printf '%s\n' "$P14_MENUDE" | grep -qx -- "$y" || {
    P14_TUTARSIZ=$((P14_TUTARSIZ + 1)); P14_TUTARSIZ_LISTE="$P14_TUTARSIZ_LISTE basit:$y"; }
done <<EOF_P14A
$P14_BASIT_YOL
EOF_P14A
while IFS= read -r y; do
  [ -n "$y" ] || continue
  printf '%s\n' "$P14_BASIT_YOL" | grep -qx -- "$y" || {
    P14_TUTARSIZ=$((P14_TUTARSIZ + 1)); P14_TUTARSIZ_LISTE="$P14_TUTARSIZ_LISTE rozet:$y"; }
done <<EOF_P14B
$P14_ROZET
EOF_P14B
if [ "$P14_BASIT_SAYI" -lt 5 ]; then
  printf "  ✗ %-46s TARAMA YAPILMADI (%s yol) — sayac gecersiz\n" \
    "P-14: BASIT_MOD_TUTARSIZ" "$P14_BASIT_SAYI"; HATA=1
elif [ "$P14_TUTARSIZ" -eq 0 ]; then
  printf "  ✓ %-46s %s (-eq 0: %s basit yol + rozet hedefleri)\n" \
    "P-14: BASIT_MOD_TUTARSIZ" "$P14_TUTARSIZ" "$P14_BASIT_SAYI"
else
  printf "  ✗ %-46s %s (-eq 0 olmalı —%s)\n" \
    "P-14: BASIT_MOD_TUTARSIZ" "$P14_TUTARSIZ" "$P14_TUTARSIZ_LISTE"; HATA=1
fi

# ── SAYAÇ 4 (destek): MENU_ELLE_BAGLANTI = 0 ───────────────────────────────
# "Menü tek kaynaktan gelir" mekanizmasının kilidi: menüyü çizen dosyaya (YildizEkranlar.razor) elle
# yazılmış href kalırsa sayaçların hiçbiri o bağlantıyı GÖREMEZ.
# Ayrıca grup başlığı çiziminin silinmediği doğrulanır (grup başlığı > 0).
P14_ELLE=$(grep -cE 'href="/' "$P14_KENAR" || true)
P14_BASLIK=$(grep -cE 'ekranlar-grup-baslik' "$P14_KENAR" || true)
if [ "$P14_ELLE" -eq 0 ] && [ "$P14_BASLIK" -ge 1 ]; then
  printf "  ✓ %-46s elle href 0 · grup başlığı çizimi var\n" "P-14: MENU_ELLE_BAGLANTI"
else
  printf "  ✗ %-46s elle href %s (0 olmalı) · grup başlığı %s (≥1 olmalı)\n" \
    "P-14: MENU_ELLE_BAGLANTI" "$P14_ELLE" "$P14_BASLIK"; HATA=1
fi

fi
# <<< P-14

# >>> S1-g — MENÜ VE ÜST BAR TUVALE UYUM (2026-09-24, kurucu: "menüyü kontrol et, olmamış")
# NEDEN: kapalı grubun 33 satırı `hidden` taşıyor ama EKRANDA görünüyordu —
#   `.kenar-satir{display:flex}` UA `[hidden]{display:none}` kuralını ezer.
#   bUnit CSS uygulamadığı için testler yeşildi: kilit CSS DOSYASINDADIR.
# SAYAÇLAR:
#   HIDDEN_GENEL_KURAL  = 1 (app.css'te `[hidden] { display: none !important; }`)
#   MENU_YILDIZ_TUTAMAC = 0 (menüde görünür yıldız/tutamaç çizimi — tema temizliği 17.10'dan beri
#                         menü = YildizUstMenu.razor + YildizEkranlar.razor; KenarCubugu kalktı)
#   UST_BAR_METIN_IKON  = 0 (UstBar.razor'da '⟳' metin ikonu)
S1G_CSS="$WEB/wwwroot/css/app.css"
S1G_HIDDEN=$(grep -cE '^\[hidden\] \{ display: none !important; \}' "$S1G_CSS" 2>/dev/null | tr -d ' ')
S1G_YILDIZ=$(cat "$WEB/Bilesenler/Kabuk/YildizUstMenu.razor" "$WEB/Bilesenler/Yildiz/YildizEkranlar.razor" 2>/dev/null \
  | grep -cE 'class="kenar-tutamac"|YildizDugmesi\(oge\)|>⠿<' | tr -d ' ')
S1G_IKON=$(grep -c '>⟳<' "$WEB/Layout/UstBar.razor" 2>/dev/null | tr -d ' ')
if [ "$S1G_HIDDEN" -ge 1 ] && [ "$S1G_YILDIZ" -eq 0 ] && [ "$S1G_IKON" -eq 0 ]; then
  printf "  ✓ %-46s [hidden] kuralı %s · menü yıldız/tutamaç %s · üst bar metin ikon %s\n" \
    "S1-g: MENU_TUVAL_UYUM" "$S1G_HIDDEN" "$S1G_YILDIZ" "$S1G_IKON"
else
  printf "  ✗ %-46s [hidden] kuralı %s (≥1) · menü yıldız/tutamaç %s (0) · üst bar metin ikon %s (0)\n" \
    "S1-g: MENU_TUVAL_UYUM" "$S1G_HIDDEN" "$S1G_YILDIZ" "$S1G_IKON"; HATA=1
fi
# <<< S1-g

# >>> P-15 — KABUK: GENEL ARAMA · KOMUT PALETİ · ŞUBE SEÇİCİ · KAYIT SEÇİCİ · DERİN BAĞLANTI
#     (cırcır; PLAN § 2 P-15, kimlikler Y-29 · Y-5 · B-25 · B-29)
#
# NEDEN: üç kusur da DERLEME HATASI VERMEZ ve testten de kolayca kaçar.
#   1) `?limit=200` ile dolan düz açılır liste 201. kaydı SESSİZCE yok sayar.
#   2) Sorgu dizesiyle gelen süzgeç hedef sayfa okumazsa SESSİZCE düşer;
#      kullanıcı bir müşteriye tıklar, bütün listeyi görür.
#   3) 26 haneli ULID isteyen bir girdi "çalışıyor" görünür, kimse dolduramaz.
# DİKKAT: betik BASH; ${=VAR} zsh sözdizimidir, burada DİZİ kullanılır.

P15_SINIF="duz-select-limit.txt"
P15_EKSIK=0
for f in "$P15_SINIF" "$WEB/Bilesenler/KayitSecici.razor" \
         "$WEB/Bilesenler/GlobalArama.razor" "$WEB/Bilesenler/SubeSecici.razor" \
         "$WEB/Bilesenler/DerinBaglanti.cs" "$API/Modules/Arama/AramaModulu.cs"; do
  [ -f "$f" ] || { printf "  ✗ %-46s %s YOK\n" "P-15: KAYNAK DOSYA" "$f"; HATA=1; P15_EKSIK=1; }
done

if [ "$P15_EKSIK" = 0 ]; then

# ── SAYAÇ 1: DUZ_SELECT_LIMIT ──────────────────────────────────────────────
# KURAL (P-15 DoD): "limit=200 düz açılır listeler kalkar; seçici sunucuda
#   arar ve sayfalar." Aynı paketin DoD'u Islemler.razor'a dokunmayı YASAKLAR
#   ve dosya ayak izi Faturalar'da yalnız #ff-fis alanını açar; bu yüzden
#   MUTLAK 0 bu pakette ULAŞILAMAZ (uygulama raporu § Sapmalar). Sayaç CIRCIR:
#   her nokta duz-select-limit.txt'te SINIFLANDIRILMIŞ olmak zorundadır ve
#   ACIK nokta sayısı ARTAMAZ (yalnız azalır).
# ÖLÇÜM KOMUTU (BK-07 — taban BELGEDEN KOPYALANMADI, burada taze koşuyor):
#   grep -rcoE 'limit=200' --include='*.razor' "$WEB/Pages"   (dosya başına adet)
P15_ACIK_TAVAN=9          # 2026-09-21 ölçümü; ARTIRILMAZ
P15_TOPLAM_TAVAN=12       # ölçülen toplam `limit=200` noktası; ARTIRILMAZ

P15_OLCULEN=$(grep -rhoE 'limit=200' --include='*.razor' "$WEB/Pages" | wc -l | tr -d ' ')
P15_TABLO_TOPLAM=$(awk -F'|' '!/^#/ && NF>=3 {t+=$3} END{print t+0}' "$P15_SINIF")
P15_TABLO_ACIK=$(awk -F'|' '/^ACIK\|/ {t+=$3} END{print t+0}' "$P15_SINIF")

if [ "$P15_OLCULEN" -le "$P15_TOPLAM_TAVAN" ] \
   && [ "$P15_OLCULEN" -eq "$P15_TABLO_TOPLAM" ] \
   && [ "$P15_TABLO_ACIK" -le "$P15_ACIK_TAVAN" ]; then
  printf "  ✓ %-46s açık %s (tavan %s) · toplam %s = tablo %s\n" \
    "P-15: DUZ_SELECT_LIMIT" "$P15_TABLO_ACIK" "$P15_ACIK_TAVAN" "$P15_OLCULEN" "$P15_TABLO_TOPLAM"
else
  printf "  ✗ %-46s ölçülen %s · tablo %s · açık %s (tavan %s)\n" \
    "P-15: DUZ_SELECT_LIMIT" "$P15_OLCULEN" "$P15_TABLO_TOPLAM" "$P15_TABLO_ACIK" "$P15_ACIK_TAVAN"
  printf "      → sınıflandırılmamış yeni `limit=200` noktası olabilir: %s\n" "$P15_SINIF"
  HATA=1
fi

# ── SAYAÇ 2: SORGU_SUZGECI_OKUMAYAN_SAYFA = 0 ──────────────────────────────
# KURAL (B-29 derin bağlantı sözleşmesi): ARAYÜZ bağlantısı (`href=` /
#   `NavigateTo(`) ya da arama ucunun ürettiği rota bir sorgu dizesi taşıyorsa,
#   HEDEF SAYFA o sorguyu OKUMAK ZORUNDADIR. Okuma kanıtı üçünden biridir:
#   DerinBaglanti. · SupplyParameterFromQuery · SorgudanSuzgecOku
# ÖLÇÜM KOMUTU: href/NavigateTo içindeki `/yol?` parçaları + AramaModulu'ndaki
#   rota dizgeleri toplanır; her yol `@page "<yol>"` ile dosyaya eşlenir.
# TAVAN: MUTLAK SIFIR.
P15_HEDEFLER=$( { grep -rhoE 'href="@?\(?\$?"?/[a-zA-Z0-9/-]+\?' --include='*.razor' "$WEB/Pages" "$WEB/Bilesenler" "$WEB/Layout"
                  grep -rhoE 'NavigateTo\(\$?"/[a-zA-Z0-9/-]+\?'  --include='*.razor' --include='*.cs' "$WEB"
                  grep -rhoE '"/[a-zA-Z0-9/-]+\?[a-zA-Z]+='       --include='*.cs' "$API/Modules/Arama"; } 2>/dev/null \
                | grep -oE '/[a-zA-Z0-9/-]+\?' | sed 's/?$//' | sort -u)

P15_OKUMAYAN=0
for yol in $P15_HEDEFLER; do
  # Hedef sayfayı @page yönergesinden bul; rota bir SAYFA değilse (API yolu) atla.
  dosya=$(grep -rlE "^@page \"$yol\"$" --include='*.razor' "$WEB/Pages" 2>/dev/null | head -1)
  [ -n "$dosya" ] || continue
  # ⚠ KANIT = ÇAĞRI, TANIM DEĞİL. İlk sürüm yalnız `SorgudanSuzgecOku` adını
  #   arıyordu; metot dosyada DURURKEN çağrısı silinince sayaç YEŞİL kalıyordu
  #   (mutasyon M15-8 bunu yakaladı). Artık çağrı satırı aranır.
  if ! grep -qE 'SupplyParameterFromQuery|^[[:space:]]*SorgudanSuzgecOku\(\);' "$dosya"; then
    printf "  ✗ %-46s %s sorgu süzgecini OKUMUYOR (%s)\n" \
      "P-15: SORGU_SUZGECI_OKUMAYAN_SAYFA" "$yol" "$dosya"
    P15_OKUMAYAN=$((P15_OKUMAYAN + 1))
    HATA=1
  fi
done
P15_HEDEF_SAYI=$(printf '%s\n' "$P15_HEDEFLER" | grep -c '^/' || true)
if [ "$P15_OKUMAYAN" -eq 0 ]; then
  printf "  ✓ %-46s 0 (taranan rota %s)\n" "P-15: SORGU_SUZGECI_OKUMAYAN_SAYFA" "$P15_HEDEF_SAYI"
fi

# ── SAYAÇ 3: HAM_ULID_GIRISI = 0 ───────────────────────────────────────────
# KURAL (B-25): kullanıcıdan 26 haneli bir kimlik YAZMASI istenemez; kayıt
#   KayitSecici ile seçilir. Ölçü: `Id` ile biten bir alana bağlanan düz
#   <input>. (RFID gibi `id` ile biten AMA kimlik OLMAYAN alanlar büyük 'I'
#   kuralıyla dışarıda kalır: `@bind="yeniRfid"` eşleşmez.)
# ÖLÇÜM KOMUTU (taze):
#   grep -rcE '<input[^>]*@bind="[A-Za-z]*Id"' --include='*.razor' Pages Bilesenler Layout
# TAVAN: MUTLAK SIFIR.
P15_ULID=$(grep -rhE '<input[^>]*@bind="[A-Za-z]*Id"' --include='*.razor' \
             "$WEB/Pages" "$WEB/Bilesenler" "$WEB/Layout" 2>/dev/null | wc -l | tr -d ' ')
if [ "$P15_ULID" -eq 0 ]; then
  printf "  ✓ %-46s 0\n" "P-15: HAM_ULID_GIRISI"
else
  printf "  ✗ %-46s %s (0 olmalı)\n" "P-15: HAM_ULID_GIRISI" "$P15_ULID"
  grep -rnE '<input[^>]*@bind="[A-Za-z]*Id"' --include='*.razor' \
    "$WEB/Pages" "$WEB/Bilesenler" "$WEB/Layout" | cut -c1-110 | sed 's/^/      → /'
  HATA=1
fi

# ── SAYAÇ 4 (destek): ARAMA_ELLE_KIRACI_SUZGECI = 0 ────────────────────────
# Arama ucu kendi kiracı süzgecini KURMAZ (kapsam P-04 global filtresidir).
# Elle `TenantId ==` yazılırsa çapraz kiracı riski sessizce geri gelir.
P15_ELLE=$(grep -cE 'TenantId ==' "$API/Modules/Arama/AramaModulu.cs" || true)
if [ "$P15_ELLE" -eq 0 ]; then
  printf "  ✓ %-46s 0\n" "P-15: ARAMA_ELLE_KIRACI_SUZGECI"
else
  printf "  ✗ %-46s %s (0 olmalı)\n" "P-15: ARAMA_ELLE_KIRACI_SUZGECI" "$P15_ELLE"; HATA=1
fi

fi
# <<< P-15





# >>> P-16 — SATIŞ KALEMİ + FORM ALANI PARİTESİ (kimlikler Y-4 · Y-26 · Y-27 · B-31 · B-69)
#     (cırcır; PLAN § 2 P-16, ilke § 1 md.1 "mekanizma önce", md.3 "sayacı olmayan paket kapanmaz")
#
# NEDEN: P-10 sayacı beş (DTO, ekran) çiftini denetliyordu. P-16 sunucuya 13 yeni
# kolon ve İKİ YENİ DTO ALANI (satirIskontoYuzde · fisIskontoYuzde) ekledi.
# Kapsam GENİŞLETİLMEZSE yeni alanlar sayacın DIŞINDA kalır ve "uçta var,
# ekranda yok" bir daha sessizce doğar. PLAN P-16: "DTO_FORM_ESLESMEZ = 0
# (kapsam genişler)".
#
# ÖLÇÜM KOMUTU (taban BELGEDEN KOPYALANMADI, burada taze koşuyor — P-10'un
# P10_ALANLAR/P10_KONTROL fonksiyonları YENİDEN KULLANILIR, ikinci kalıp açılmaz):
#   P10_KONTROL "<kaynak>" "<DTO sınıfı>" "<razor>"
#   2026-09-21 taze ölçüm: 7 çift / 0 eşleşmeyen alan.
P16_ONCE_ESLESMEZ=$P10_ESLESMEZ
P16_ONCE_CIFT=$P10_CIFT_SAYISI
P10_KONTROL "$API/Modules/Satis/SatisModulu.cs" SatisFisKalemDto "$WEB/Pages/Islemler.razor"
P10_KONTROL "$API/Modules/Satis/SatisModulu.cs" SatisFisiDto     "$WEB/Pages/Islemler.razor"
P16_CIFT=$(( P10_CIFT_SAYISI - P16_ONCE_CIFT ))
P16_ESLESMEZ=$(( P10_ESLESMEZ - P16_ONCE_ESLESMEZ ))

if [ "$P10_CIFT_SAYISI" -lt 7 ]; then
  printf "  x %-46s %s cift (>=7 olmali - P-16 kapsami DARALTILAMAZ)\n" \
    "P-16: DTO_FORM_ESLESMEZ kapsami" "$P10_CIFT_SAYISI"; HATA=1
elif [ "$P16_ESLESMEZ" -eq 0 ]; then
  printf "  ✓ %-46s %s (-eq 0, +%s çift · toplam %s çift)\n" \
    "P-16: DTO_FORM_ESLESMEZ" "$P16_ESLESMEZ" "$P16_CIFT" "$P10_CIFT_SAYISI"
else
  printf "  ✗ %-46s %s (-eq 0 olmalı — satış fişi alanı formda YOK)\n" \
    "P-16: DTO_FORM_ESLESMEZ" "$P16_ESLESMEZ"
  printf "      →%s\n" "$P10_ESLESMEZ_LISTE"; HATA=1
fi

# ── BU_PAKETTE_EKLENEN_RECORD = 0 (cırcır; PLAN § 1 md.9) ───────────────────
# P-16 üç razor sayfasına dokundu (Islemler · Urunler · Cariler) ve HİÇBİRİNE
# istemci record'u eklemedi: yeni alanlar Kuyumcu.Contracts'taki MEVCUT
# tiplere (UrunVeri · UrunTamKart · CariDetayVeri) eklendi.
# ÖLÇÜM KOMUTU:
#   grep -c 'record ' Kuyumcu.Web/Pages/Islemler.razor Kuyumcu.Web/Pages/Urunler.razor \
#                     Kuyumcu.Web/Pages/Cariler.razor
# 2026-09-21 taze ölçüm: 0.
P16_REC=$(grep -hcE '\brecord [A-Za-z0-9_]+' \
            "$WEB/Pages/Islemler.razor" "$WEB/Pages/Urunler.razor" "$WEB/Pages/Cariler.razor" \
            2>/dev/null | awk '{t+=$1} END{print t+0}')
if [ "$P16_REC" -eq 0 ]; then
  printf "  ✓ %-46s 0 (Islemler+Urunler+Cariler)\n" "P-16: BU_PAKETTE_EKLENEN_RECORD"
else
  printf "  ✗ %-46s %s (0 olmalı — istemci record'u Contracts'a yazılır)\n" \
    "P-16: BU_PAKETTE_EKLENEN_RECORD" "$P16_REC"; HATA=1
fi

# ── B-31 mekanizması: pasif cari SATIŞTA hem ekranda hem SUNUCUDA kapalı ───
# "Ekran gizliyor" yetmez: doğrudan API çağrısı ekranı atlar (fail-closed).
P16_B31_EKRAN=$(grep -c 'aktif=true' "$WEB/Pages/Islemler.razor" 2>/dev/null || true)
P16_B31_SUNUCU=$(grep -c 'CariAktifDogrula' "$API/Modules/Satis/SatisModulu.cs" 2>/dev/null || true)
if [ "$P16_B31_EKRAN" -ge 1 ] && [ "$P16_B31_SUNUCU" -ge 2 ]; then
  printf "  ✓ %-46s ekran %s · sunucu kapısı %s\n" \
    "P-16: PASIF_CARI_SATISTA" "$P16_B31_EKRAN" "$P16_B31_SUNUCU"
else
  printf "  ✗ %-46s ekran %s · sunucu kapısı %s (ikisi de gerekli)\n" \
    "P-16: PASIF_CARI_SATISTA" "$P16_B31_EKRAN" "$P16_B31_SUNUCU"; HATA=1
fi

# ── Y-4 mekanizması: iskonto TUTARI istemciden ALINMAZ ─────────────────────
# Sözleşmede yalnız YÜZDE vardır; bir gün "satirIskontoTl" eklenirse bu sayaç
# kırılır ve ekran ile defter ayrışması yeniden mümkün olmaz.
P16_ISK_TL=$(grep -cE 'SatirIskontoTl|FisIskontoTl' "$API/Modules/Satis/SatisModulu.cs" 2>/dev/null || true)
if [ "$P16_ISK_TL" -eq 0 ]; then
  printf "  ✓ %-46s 0 (yalnız yüzde taşınır, tutar sunucuda hesaplanır)\n" "P-16: ISKONTO_TUTARI_ISTEMCIDEN"
else
  printf "  ✗ %-46s %s (iskonto TUTARI istemciden alınamaz)\n" "P-16: ISKONTO_TUTARI_ISTEMCIDEN" "$P16_ISK_TL"; HATA=1
fi
# <<< P-16

# >>> P-17 — ORTAK ÇIKTI ŞABLONU + PANO/VİTRİN (kimlikler Y-6 · Y-7)
#
# NEDEN CIRCIR: "tek A4 şablonu" bir SÖZ değil, ölçülebilir bir kısıt olmalı.
# Beşinci belge yazan kişi kendi kâğıt düzenini açmaya kalkarsa bu blok kırılır.
echo
echo "── P-17: ORTAK ÇIKTI ŞABLONU + VİTRİN PANOSU ──────────────────────────"

# ── ORTAK_SABLON_DISI_PRINT ∈ gerekçeli beyaz liste ────────────────────────
# ÖLÇÜM: grep -rlE 'InvokeVoidAsync\("window\.print"\)' --include='*.razor' \
#          Kuyumcu.Web/Pages Kuyumcu.Web/Bilesenler Kuyumcu.Web/Layout | sort
# 2026-09-21 TAZE ÖLÇÜM (belgeden kopyalanmadı, burada koşuldu):
#   ÖNCE : Pages/PusulaBelgesi.razor · Pages/IslemDefteri.razor · Pages/UrunEtiketleri.razor  (3)
#   SONRA: Bilesenler/Yazdirma/YazdirDugmesi.razor · Pages/UrunEtiketleri.razor               (2)
# BEYAZ LİSTE (gerekçeli, YALNIZ AZALIR):
#   • Bilesenler/Yazdirma/YazdirDugmesi.razor — ORTAK şablonun kendi çağrısı.
#   • Pages/UrunEtiketleri.razor — TERMAL / BARKOD ailesi. A4 değildir; kendi
#     @page size kuralı, kesme payı ve etiket ızgarası vardır. Plan § 2 P-17
#     DoD bunu açıkça istisna tutar ("3 → 0 hedefi yanlıştı", hasım bulgusu).
# DİKKAT: betik BASH. ${=VAR} zsh sözdizimidir; bash'te "bad substitution" verip
# döngüyü SESSİZCE boş bırakır → sayaç yalancı 0 döner. Bu yüzden DİZİ kullanılır.
P17_BEYAZ=( "$WEB/Bilesenler/Yazdirma/YazdirDugmesi.razor" "$WEB/Pages/UrunEtiketleri.razor" )
P17_PRINT=()
while IFS= read -r _satir; do
  [ -n "$_satir" ] && P17_PRINT+=( "$_satir" )
done < <(grep -rlE 'InvokeVoidAsync\("window\.print"\)' --include='*.razor' \
              "$WEB/Pages" "$WEB/Bilesenler" "$WEB/Layout" 2>/dev/null | sort || true)
P17_DISI=0
for f in "${P17_PRINT[@]}"; do
  _beyazmi=0
  for b in "${P17_BEYAZ[@]}"; do [ "$f" = "$b" ] && _beyazmi=1; done
  [ "$_beyazmi" = 1 ] || { P17_DISI=$((P17_DISI + 1)); echo "      ↳ beyaz liste dışı: $f"; }
done
if [ "$P17_DISI" -eq 0 ]; then
  printf "  ✓ %-46s 0 (beyaz liste: 2 — ortak düğme + termal etiket)\n" "P-17: ORTAK_SABLON_DISI_PRINT"
else
  printf "  ✗ %-46s %s (yazdırma YALNIZ Bilesenler/Yazdirma'dan geçer)\n" \
    "P-17: ORTAK_SABLON_DISI_PRINT" "$P17_DISI"; HATA=1
fi

# ── BEYAZ LİSTE BAYAT DEĞİL: listedeki her dosya GERÇEKTEN print çağırıyor ──
# Muafiyet yazıp dosyayı değiştirmek "gerekçeli istisna"yı sessizce genişletir.
P17_BAYAT=0
for f in "${P17_BEYAZ[@]}"; do
  grep -qE 'InvokeVoidAsync\("window\.print"\)' "$f" 2>/dev/null || {
    P17_BAYAT=$((P17_BAYAT + 1)); echo "      ↳ bayat muafiyet: $f"; }
done
if [ "$P17_BAYAT" -eq 0 ]; then
  printf "  ✓ %-46s 0\n" "P-17: PRINT_BEYAZ_LISTE_BAYAT"
else
  printf "  ✗ %-46s %s (muafiyet gerçeği yansıtmıyor)\n" "P-17: PRINT_BEYAZ_LISTE_BAYAT" "$P17_BAYAT"; HATA=1
fi

# ── TEK A4 ŞABLONU: kâğıt düzeni TEK bileşende ─────────────────────────────
# ÖLÇÜM: grep -rlE '<article class="belge"' --include='*.razor' Kuyumcu.Web
# 2026-09-21 taze ölçüm: 2 (A4Belge.razor = ortak şablon · PusulaBelgesi.razor
# = VUK md.234'e özgü sabit düzen, 1.0'da ortak şablona GİRMEZ).
P17_SABLON=$(grep -rlE '<article class="belge"' --include='*.razor' "$WEB" 2>/dev/null | grep -c . || true)
P17_SABLON_TAVAN=2
if [ "$P17_SABLON" -le "$P17_SABLON_TAVAN" ]; then
  printf "  ✓ %-46s %s/%s (tavan YALNIZ azalır)\n" "P-17: A4_KAGIT_DUZENI_SAYISI" "$P17_SABLON" "$P17_SABLON_TAVAN"
else
  printf "  ✗ %-46s %s/%s (yeni belge KENDİ düzenini açamaz)\n" \
    "P-17: A4_KAGIT_DUZENI_SAYISI" "$P17_SABLON" "$P17_SABLON_TAVAN"; HATA=1
fi

# ── DÖRT BELGE TÜRÜ DE ORTAK SÖZLEŞMEDEN ÇIKAR ─────────────────────────────
# ÖLÇÜM: grep -cE 'public const string (SatisFisi|TahsilatMakbuzu|EmanetTeslim|CariEkstre)' \
#          Kuyumcu.Contracts/Cikti/CiktiSozlesmeleri.cs
P17_TUR=$(grep -cE 'public const string (SatisFisi|TahsilatMakbuzu|EmanetTeslim|CariEkstre) =' \
            Kuyumcu.Contracts/Cikti/CiktiSozlesmeleri.cs 2>/dev/null || true)
if [ "$P17_TUR" -eq 4 ]; then
  printf "  ✓ %-46s 4 (fiş · makbuz · emanet · ekstre)\n" "P-17: ORTAK_BELGE_TURU"
else
  printf "  ✗ %-46s %s (4 olmalı — DoD dört belgeyi tek şablona bağlar)\n" "P-17: ORTAK_BELGE_TURU" "$P17_TUR"; HATA=1
fi

# ── PANO AYARI KİRACI AYARINDAN GELİR (yeni tablo YOK) ─────────────────────
# ÖLÇÜM: grep -c 'KiraciAyarlari' Kuyumcu.Api/Modules/Pano/PanoModulu.cs
#        grep -c 'PanoAyarServisi' Kuyumcu.Api/Modules/ModulKayit.cs
P17_PANO_AYAR=$(grep -c 'db.KiraciAyarlari' "$API/Modules/Pano/PanoModulu.cs" 2>/dev/null || true)
P17_PANO_KAYIT=$(grep -c 'Pano.PanoAyarServisi' "$API/Modules/ModulKayit.cs" 2>/dev/null || true)
if [ "$P17_PANO_AYAR" -ge 1 ] && [ "$P17_PANO_KAYIT" -eq 1 ]; then
  printf "  ✓ %-46s kiraci_ayarlari %s · modül kaydı %s\n" \
    "P-17: PANO_AYAR_KIRACIDAN" "$P17_PANO_AYAR" "$P17_PANO_KAYIT"
else
  printf "  ✗ %-46s kiraci_ayarlari %s · modül kaydı %s (ikisi de gerekli)\n" \
    "P-17: PANO_AYAR_KIRACIDAN" "$P17_PANO_AYAR" "$P17_PANO_KAYIT"; HATA=1
fi

# ── PANO HESABI TEK YERDE: ikinci marj/yuvarlama yolu açılamaz ─────────────
# ÖLÇÜM: grep -rc 'YuvarlamaAdimi \*\|Math.Ceiling\|Math.Floor' Kuyumcu.Api/Modules/Pano
# Saf servis dışında hiçbir yerde yuvarlama aritmetiği olmamalı.
P17_IKINCI_HESAP=$(grep -rlE 'Math\.(Ceiling|Floor)' --include='*.cs' "$API/Modules/Pano" 2>/dev/null \
                   | grep -v 'PanoFiyatServisi.cs' | grep -c . || true)
if [ "$P17_IKINCI_HESAP" -eq 0 ]; then
  printf "  ✓ %-46s 0 (hesap YALNIZ PanoFiyatServisi'nde)\n" "P-17: PANO_IKINCI_HESAP_YOLU"
else
  printf "  ✗ %-46s %s (marj/yuvarlama ikinci kez yazılmış)\n" "P-17: PANO_IKINCI_HESAP_YOLU" "$P17_IKINCI_HESAP"; HATA=1
fi

# ── MARJ VİTRİNE İNMEZ: pano görünüm sözleşmesinde marj alanı YOK ──────────
# ÖLÇÜM: awk 'record PanoSatirGorunum|record PanoGorunum' bloklarında 'Marj' ara.
P17_MARJ_SIZINTI=$(grep -E 'record (PanoSatirGorunum|PanoGorunum)\(' -A 12 \
                     Kuyumcu.Contracts/Pano/PanoSozlesmeleri.cs 2>/dev/null \
                   | grep -c 'Marj' || true)
if [ "$P17_MARJ_SIZINTI" -eq 0 ]; then
  printf "  ✓ %-46s 0 (marj ticari sırdır, panoya inmez)\n" "P-17: PANO_MARJ_SIZINTISI"
else
  printf "  ✗ %-46s %s (marj vitrin sözleşmesine sızmış)\n" "P-17: PANO_MARJ_SIZINTISI" "$P17_MARJ_SIZINTI"; HATA=1
fi
# <<< P-17

# >>> P-18 — MOBİL PARİTE · TEMA BELİRTEÇLERİ · STICKY SÜTUN
#     (kimlikler B-9 · B-30 · B-32; PLAN § 2 P-18, BK-24; risk: CSS gerilemesi)
#
# İLKE (PLAN § 1 md.1 "MEKANİZMA ÖNCE" + md.2 "taban ölçülür, kopyalanmaz"):
# 375 px genişliğin KENDİSİ kabuk betiğiyle ölçülemez — onu yalnız gerçek
# tarayıcı ölçer (betikler/mobil-olc.js, BK-24 "örnekleme kabul edilmez").
# Bu blok o ölçümün ÖN KOŞULLARINI ve GERİLEME YOLLARINI kapatır; sayısal
# 375 iddiası ölçüm dosyasından OKUNUR, burada uydurulmaz.
P18_KAYIT="../dokumanlar/09-uygulama-kayitlari"
P18_OLC_BETIK="$P18_KAYIT/betikler/mobil-olc.js"
P18_OLC_A="$P18_KAYIT/olcumler/P-18-mobil-375-aydinlik.json"
P18_OLC_K="$P18_KAYIT/olcumler/P-18-mobil-375-karanlik.json"

# ── 1) MOBIL_YATAY_KAYDIRMA = 0 (gerçek tarayıcı ölçümünden OKUNUR) ─────────
# ÖLÇÜM KOMUTU (tazelemek için):
#   cd dotnet && ASPNETCORE_ENVIRONMENT=Development dotnet run --project Kuyumcu.Api \
#     --no-build --no-launch-profile --urls http://localhost:5198 &
#   node ../dokumanlar/09-uygulama-kayitlari/betikler/mobil-olc.js aydinlik cikti.json
# Geçme ölçütü: hiçbir rotanın "sw" değeri 375'ten büyük olmayacak.
# ⚠ Bu dosya bir ANLIK GÖRÜNTÜDÜR; tazeliği QA turu kapısında zorlanır
#   (betikler/qa-turu.js → MOBIL_YATAY_KAYDIRMA). Burada yalnız "son ölçüm
#   sıfırdı" iddiası makineyle doğrulanır; "ölçüm yok" da FAIL'dir.
if [ ! -f "$P18_OLC_BETIK" ]; then
  printf "  ✗ %-46s ölçüm betiği YOK (%s)\n" "P-18: MOBIL_OLCUM_BETIGI" "$P18_OLC_BETIK"; HATA=1
else
  printf "  ✓ %-46s %s\n" "P-18: MOBIL_OLCUM_BETIGI" "mobil-olc.js (gerçek Chrome/CDP, tek tek)"
fi
P18_TASAN=0; P18_ROTA=0; P18_OLCUM_VAR=1
for P18_D in "$P18_OLC_A" "$P18_OLC_K"; do
  if [ ! -f "$P18_D" ]; then P18_OLCUM_VAR=0; continue; fi
  P18_ROTA=$(( P18_ROTA + $(grep -c '"rota":' "$P18_D" 2>/dev/null || echo 0) ))
  P18_TASAN=$(( P18_TASAN + $(grep -oE '"sw": [0-9]+' "$P18_D" 2>/dev/null \
                | awk '{ if ($2 > 375) n++ } END { print n+0 }') ))
done
if [ "$P18_OLCUM_VAR" -eq 1 ] && [ "$P18_TASAN" -eq 0 ] && [ "$P18_ROTA" -ge 84 ]; then
  printf "  ✓ %-46s 0 / %s rota-ölçüm (aydınlık + karanlık, tek tek)\n" \
    "P-18: MOBIL_YATAY_KAYDIRMA" "$P18_ROTA"
else
  printf "  ✗ %-46s %s taşan / %s rota-ölçüm (dosya var mı: %s)\n" \
    "P-18: MOBIL_YATAY_KAYDIRMA" "$P18_TASAN" "$P18_ROTA" "$P18_OLCUM_VAR"; HATA=1
fi

# ── 2) KABUK SARMA KURALLARI (gerilemenin ASIL yolu) ───────────────────────
# ÖLÇÜM: 2026-09-21 taze ölçüm — bu 4 kural yokken 44/44 rota 565-814 px idi.
#   grep -c 'flex-wrap: wrap' app.css  (.ust-bar/.ust-sag/.sube-secici) + .ust-bar h1 min-width
P18_KABUK=0
grep -qE '^\s*\.ust-bar \{ flex-wrap: wrap;' "$WEB/wwwroot/css/app.css" && P18_KABUK=$((P18_KABUK+1))
grep -qE '^\s*\.ust-bar h1 \{.*min-width: 0'  "$WEB/wwwroot/css/app.css" && P18_KABUK=$((P18_KABUK+1))
grep -qE '^\s*\.ust-sag \{ flex-wrap: wrap;'  "$WEB/wwwroot/css/app.css" && P18_KABUK=$((P18_KABUK+1))
grep -qE '^\s*\.sube-secici \{ flex-wrap: wrap;' "$WEB/wwwroot/css/app.css" && P18_KABUK=$((P18_KABUK+1))
grep -qE '^\.tablo-sar \{ position: relative; \}' "$WEB/wwwroot/css/app.css" && P18_KABUK=$((P18_KABUK+1))
if [ "$P18_KABUK" -eq 5 ]; then
  printf "  ✓ %-46s 5/5 (üst bar sarar · başlık küçülür · kutu hapseder)\n" "P-18: MOBIL_KABUK_SARMA"
else
  printf "  ✗ %-46s %s/5 (dar ekran kabuk kuralı eksik → 44 rota birden taşar)\n" \
    "P-18: MOBIL_KABUK_SARMA" "$P18_KABUK"; HATA=1
fi

# ── 3) TAŞMA GİZLENMEZ (Anayasa md.1) ──────────────────────────────────────
# ÖLÇÜM: kabuk zeminine overflow-x:hidden yazıp "375'e sığdı" demek YASAK.
P18_GIZLEME=$(grep -nE '(\.ana-icerik|\.icerik-sutunu|\.uygulama|^body|^html)[^{]*\{[^}]*overflow(-x)?: *hidden' \
                "$WEB/wwwroot/css/app.css" 2>/dev/null | grep -c . || true)
if [ "$P18_GIZLEME" -eq 0 ]; then
  printf "  ✓ %-46s 0 (taşma gizlenmedi, hapsedildi)\n" "P-18: MOBIL_TASMA_GIZLEME"
else
  printf "  ✗ %-46s %s (kabuk zemininde overflow:hidden)\n" "P-18: MOBIL_TASMA_GIZLEME" "$P18_GIZLEME"; HATA=1
fi

# ── 4) TEK SARMALAYICI KALIBI: ikinci "tablo-sar" eli açılmaz ──────────────
# ÖLÇÜM: P-18'in sahiplendiği 6 ekranda elle <div class="tablo-sar"> KALMADI.
# ⚠ BASH dizisi: ${=VAR} zsh sözdizimidir, bash'te sayacı ÖLDÜRÜR (P-17 rev 1.1 dersi).
P18_SAHIP=( Cariler Urunler Isletme Banka Islemler Yetkiler )
P18_SARSIZ=0
for P18_S in "${P18_SAHIP[@]}"; do
  # ⚠ 2026-09-21: `grep -c … || echo 0` BOZUKTU — grep eşleşme bulamayınca "0" BASAR
  #   ve 1 ile çıkar, yani `|| echo 0` ikinci bir "0" ekliyordu; $(( )) "0\n0" görüp
  #   "syntax error in expression" veriyordu. Sayaç böylece ÖLÇMÜYORDU (Madde 1).
  P18_N=$(grep -c '<div class="tablo-sar">' "$WEB/Pages/$P18_S.razor" 2>/dev/null)
  P18_SARSIZ=$(( P18_SARSIZ + ${P18_N:-0} ))
done
# PAKET GOC-A (08.10): ortak ızgara (Bilesenler/Grid/Izgara.razor) KartListe'yi İÇİNDEN kullanır — sarmalayıcı yine
# TEK kalıptır; ızgaraya taşınan listeler (<Izgara …> ya da tam adla) KartListe kullanımı sayılır (GRID-1 test kuralıyla aynı).
P18_KARTLISTE=$(grep -rcE '<KartListe|<Izgara |<Kuyumcu\.Web\.Bilesenler\.Grid\.Izgara ' --include='*.razor' "$WEB/Pages" 2>/dev/null | awk -F: '{s+=$2} END {print s+0}')
if [ "$P18_SARSIZ" -eq 0 ] && [ "$P18_KARTLISTE" -ge 9 ]; then
  printf "  ✓ %-46s 0 elle sarmalayıcı · %s KartListe kullanımı\n" \
    "P-18: MOBIL_TABLO_SARMALAYICISIZ" "$P18_KARTLISTE"
else
  printf "  ✗ %-46s %s elle sarmalayıcı · %s KartListe (ikinci kalıp açılmış)\n" \
    "P-18: MOBIL_TABLO_SARMALAYICISIZ" "$P18_SARSIZ" "$P18_KARTLISTE"; HATA=1
fi

# ── 5) SABIT_RENK (cırcır; YALNIZ AZALIR) ──────────────────────────────────
# ÖLÇÜM KOMUTU:
#   grep -rhoE '#[0-9a-fA-F]{3,8}\b|rgba?\([0-9 ,.%/]+\)' --include='*.razor' \
#     Pages Bilesenler Layout | grep -c .
# 2026-09-21 TAZE ÖLÇÜM: P-18 öncesi 31 → sonrası 14. Kalan 14'ün TAMAMI
# Pages/Giris.razor'daki tanıtım panelidir; o panel belirtecler.css'te BİLİNÇLİ
# olarak her iki temada da koyudur (--giris-panel-*), tema değişkeni değildir.
# Tavan ARTIRILAMAZ.
# 2026-09-21 (P-36c/3, TAZE): 14 → **0**. Giriş illüstrasyonunun SVG gradyan
# durakları belirtece çevrildi (--giris-altin-* / --giris-tas-* / --giris-parilti
# …, belirtecler.css). Razor içinde sabit renk KALMADI.
P18_RENK_TAVAN=0
P18_RENK=$(grep -rhoE '#[0-9a-fA-F]{3,8}\b|rgba?\([0-9 ,.%/]+\)' --include='*.razor' \
             "$WEB/Pages" "$WEB/Bilesenler" "$WEB/Layout" 2>/dev/null | grep -c . || true)
P18_RENK_DIS=$(grep -rloE '#[0-9a-fA-F]{3,8}\b|rgba?\([0-9 ,.%/]+\)' --include='*.razor' \
                 "$WEB/Pages" "$WEB/Bilesenler" "$WEB/Layout" 2>/dev/null \
               | grep -v 'Pages/Giris.razor' | grep -c . || true)
if [ "$P18_RENK" -le "$P18_RENK_TAVAN" ] && [ "$P18_RENK_DIS" -eq 0 ]; then
  printf "  ✓ %-46s %s (tavan %s — cırcır; razor'da sabit renk yok)\n" \
    "P-18: SABIT_RENK" "$P18_RENK" "$P18_RENK_TAVAN"
else
  printf "  ✗ %-46s %s > tavan %s · panel dışı dosya %s\n" \
    "P-18: SABIT_RENK" "$P18_RENK" "$P18_RENK_TAVAN" "$P18_RENK_DIS"; HATA=1
fi

# ── 6) B-32: vurgu yüzeyleri HAM PALETTEN gelmez ───────────────────────────
# ÖLÇÜM: .ozet-kutu--amber / .kart--masak / .kart--soluk kurallarında --amber-*
# ya da --slate-* geçemez (ham palet temayla DEĞİŞMEZ → karanlıkta okunmaz).
P18_HAM=$(grep -E '^\.(ozet-kutu--amber|kart--masak|kart--soluk)[^{]*\{[^}]*(--amber-|--slate-)' \
            "$WEB/wwwroot/css/app.css" 2>/dev/null | grep -c . || true)
if [ "$P18_HAM" -eq 0 ]; then
  printf "  ✓ %-46s 0 (vurgu yüzeyi semantik belirteçten)\n" "P-18: TEMA_HAM_PALET_VURGU"
else
  printf "  ✗ %-46s %s (karanlık temada okunmaz zemin)\n" "P-18: TEMA_HAM_PALET_VURGU" "$P18_HAM"; HATA=1
fi

# ── 7) B-30: /yetkiler ROL sütunu YAPIŞKAN ─────────────────────────────────
P18_YAPISKAN=$(grep -c 'YapiskanIlkSutun="true"' "$WEB/Pages/Yetkiler.razor" 2>/dev/null || true)
P18_YAPISKAN_CSS=$(grep -c 'kart-liste--yapiskan' "$WEB/wwwroot/css/app.css" 2>/dev/null || true)
if [ "$P18_YAPISKAN" -ge 1 ] && [ "$P18_YAPISKAN_CSS" -ge 2 ]; then
  printf "  ✓ %-46s razor %s · css %s\n" "P-18: YETKILER_ROL_YAPISKAN" "$P18_YAPISKAN" "$P18_YAPISKAN_CSS"
else
  printf "  ✗ %-46s razor %s · css %s (ikisi de gerekli)\n" \
    "P-18: YETKILER_ROL_YAPISKAN" "$P18_YAPISKAN" "$P18_YAPISKAN_CSS"; HATA=1
fi
# <<< P-18


# >>> P-19 — GİDER + VİRMAN: İKİ BACAKLI ATOMİK DESEN (Y-11 · Y-12)
echo
echo "═══ P-19 iki bacaklı desen kapısı ═══"

# ── 1) ESSIZ_BACAK = 0 (cırcır; YALNIZ AZALIR) ─────────────────────────────
# NE ÖLÇER: `KarsiBacakId` ATAMASININ fabrika DIŞINDA yapıldığı nokta sayısı.
# NEDEN: bir bacağı karşılıksız (eşsiz) yazmanın TEK yolu, eşleştirmeyi
#   CiftBacakFabrikasi.BacaklariEsle dışında elle yapmaktır. Orada yapılırsa
#   "A.karsi=B.id ve B.karsi=A.id" değişmezi kırılır, virmanın yarısı öksüz
#   kalır ve kasa/banka bakiyesi sessizce şişer (P-19'un baş riski).
#   Okuma (`h.KarsiBacakId is not null`, `h.KarsiBacakId != null`) SAYILMAZ;
#   yalnız YAZMA (`KarsiBacakId =` / `KarsiBacakId=`) sayılır.
# ÖLÇÜM KOMUTU:
#   grep -rnE 'KarsiBacakId[[:space:]]*=[^=]' Kuyumcu.Api --include='*.cs' \
#     | grep -v '/obj/\|/bin/\|/Migrations/' \
#     | grep -v 'Data/HareketFabrikasi.cs' | wc -l
# 2026-09-21 TAZE ÖLÇÜM (bu paketin uygulamasından sonra): 0.
P19_ESSIZ=$(grep -rnE 'KarsiBacakId[[:space:]]*=[^=]' "$API" --include='*.cs' 2>/dev/null \
  | grep -v '/obj/\|/bin/\|/Migrations/' \
  | grep -v 'Data/HareketFabrikasi\.cs' | wc -l | tr -d ' ')
if [ "$P19_ESSIZ" -eq 0 ]; then
  printf "  ✓ %-46s 0 (eşleştirme yalnız CiftBacakFabrikasi'nde)\n" "P-19: ESSIZ_BACAK"
else
  printf "  ✗ %-46s %s (fabrika dışı karşı-bacak ataması — yarım virman riski)\n" \
    "P-19: ESSIZ_BACAK" "$P19_ESSIZ"
  grep -rnE 'KarsiBacakId[[:space:]]*=[^=]' "$API" --include='*.cs' 2>/dev/null \
    | grep -v '/obj/\|/bin/\|/Migrations/' | grep -v 'Data/HareketFabrikasi\.cs' \
    | sed 's/^/      → /'
  HATA=1
fi

# ── 2) TEK_SAVECHANGES_DISI_CIFT_BACAK = 0 (cırcır; YALNIZ AZALIR) ─────────
# NE ÖLÇER: `BacaklariEsle(` çağıran ve o çağrının bulunduğu metotta TAM 1
#   adet `SaveChangesAsync` OLMAYAN metot sayısı.
# NEDEN: iki bacak TEK SaveChanges (dolayısıyla tek veritabanı işlemi) ile
#   yazılmalıdır. İki ayrı SaveChanges olsaydı araya giren bir hata birinci
#   bacağı kalıcı, ikincisini yazılmamış bırakırdı — transaction geri alsa bile
#   "tek bacak commit edildi" hâli kod düzeyinde MÜMKÜN olurdu. Sıfır
#   SaveChanges ise bacakların hiç yazılmaması demektir.
# ÖLÇÜM KOMUTU (awk, metot sınırı = girinti 4 imza satırı):
#   awk '/^    (public|private|internal|protected)[^;]*\(/ {...}' <her .cs>
P19_CIFT_IHLAL=0
P19_CIFT_DOSYA=$(grep -rl 'BacaklariEsle(' "$API" --include='*.cs' 2>/dev/null \
  | grep -v '/obj/\|/bin/\|/Migrations/' | grep -v 'Data/HareketFabrikasi\.cs')
# ⚠ 2026-09-21: `${=VAR}` ZSH sözdizimidir; bu betik bash ile koşuyor ve
#   "bad substitution" verip döngüyü HİÇ çalıştırmıyordu — sayaç 0 görünüyordu
#   ama ÖLÇMÜYORDU. Bash'te satır satır okunur (dosya adlarında boşluk da güvenli).
while IFS= read -r f; do
  [ -n "$f" ] || continue
  P19_CIFT_IHLAL=$(( P19_CIFT_IHLAL + $(awk '
    /^    (public|private|internal|protected|static)[^;]*\(/ {
      if (acik && esle > 0 && save != 1) ihlal++
      acik = 1; esle = 0; save = 0
    }
    /BacaklariEsle\(/ { esle++ }
    /SaveChangesAsync/ { save++ }
    END { if (acik && esle > 0 && save != 1) ihlal++; print ihlal + 0 }
  ' "$f") ))
done <<< "$P19_CIFT_DOSYA"
if [ "$P19_CIFT_IHLAL" -eq 0 ]; then
  printf "  ✓ %-46s 0 (her çift bacak tam 1 SaveChangesAsync)\n" "P-19: TEK_SAVECHANGES_DISI_CIFT_BACAK"
else
  printf "  ✗ %-46s %s metot (iki bacak tek işlemde yazılmıyor)\n" \
    "P-19: TEK_SAVECHANGES_DISI_CIFT_BACAK" "$P19_CIFT_IHLAL"; HATA=1
fi

# ── 3) Destek kapısı: tek bacaklı storno yolu KAPALI ───────────────────────
# Kasa ve banka storno yolları virman bacağını REDDETMELİDİR; aksi hâlde
# paranın yalnız yarısı geri döner. İki kapı da kodda DURUYOR mu?
P19_TEKBACAK_KAPI=$(grep -c 'KarsiBacakId is not null' "$API/Modules/Satis/StornoServisi.cs" 2>/dev/null || echo 0)
if [ "$P19_TEKBACAK_KAPI" -ge 2 ]; then
  printf "  ✓ %-46s %s kapı (kasa + banka)\n" "P-19: TEK_BACAK_STORNO_KAPISI" "$P19_TEKBACAK_KAPI"
else
  printf "  ✗ %-46s %s (kasa ve banka yollarının İKİSİ de reddetmeli)\n" \
    "P-19: TEK_BACAK_STORNO_KAPISI" "$P19_TEKBACAK_KAPI"; HATA=1
fi

# ── 4) VIRMAN_BACAGI_EKRANSIZ_KAPI = 0 (cırcır; YALNIZ AZALIR) ─────────────
# NE ÖLÇER: yukarıdaki (3) sunucu kapısının EKRAN karşılığı OLMAYAN sayfa
#   sayısı. Kasa ve banka sayfalarının HER BİRİ hem `KarsiBacakId`i tanımalı
#   (storno düğmesini kilitlemek için) hem de doğru yolu (`/gider?sekme=virman`)
#   göstermelidir.
# NEDEN: hasım doğrulayıcı bulgusu (2026-09-21, sapma S1). Sunucu kapısı tek
#   başına YETMEZ: kullanıcı kasa/banka ekranında storno düğmesine basıyor,
#   409 ham hata bandı görüyor ve doğru yola götüren hiçbir bağlantı bulamıyordu.
#   UX-S25 bunu açıkça yasaklar: "sunucuda uç var, ekranda düğme yok" KABUL EDİLMEZ.
# TABAN: 0 — TAZE ÖLÇÜLDÜ (2026-09-21), belgeden kopyalanmadı:
# ÖLÇÜM KOMUTU:
#   for f in Kasalar Banka; do
#     grep -c 'KarsiBacakId' Kuyumcu.Web/Pages/$f.razor       # → 2 · 2
#     grep -c '/gider?sekme=virman' Kuyumcu.Web/Pages/$f.razor # → 1 · 1
#   done
P19_EKRANSIZ=0
for s in Kasalar Banka; do
  P19_SAYFA="$WEB/Pages/$s.razor"
  grep -q 'KarsiBacakId' "$P19_SAYFA" 2>/dev/null || P19_EKRANSIZ=$(( P19_EKRANSIZ + 1 ))
  grep -q '/gider?sekme=virman' "$P19_SAYFA" 2>/dev/null || P19_EKRANSIZ=$(( P19_EKRANSIZ + 1 ))
done
if [ "$P19_EKRANSIZ" -eq 0 ]; then
  printf "  ✓ %-46s 0 (kasa + banka: kilit ve doğru yol ekranda)\n" "P-19: VIRMAN_BACAGI_EKRANSIZ_KAPI"
else
  printf "  ✗ %-46s %s eksik (sunucuda uç var, ekranda yol yok — UX-S25)\n" \
    "P-19: VIRMAN_BACAGI_EKRANSIZ_KAPI" "$P19_EKRANSIZ"; HATA=1
fi
# <<< P-19

# >>> P-20 — HURDA SATIŞI / RAMAT ÇIKIŞI (Y-10)
echo
echo "═══ P-20 hurda stok kapısı ═══"

# ── HURDA_STOK_KACAK_YOL = 0 (cırcır; YALNIZ AZALIR) ───────────────────────
# NE ÖLÇER: hurdanın "elde ne var" gerçeğini ATLAYAN yolların toplamı. Dört
#   bileşeni vardır ve hepsi 0 olmalıdır:
#   (a) `Tur = "HURDA_SATIS"` yazan ama `DogrulaCikis` çağırmayan metot,
#   (b) hurda tür listesini (`HurdaTurleri`) kapı DIŞINDA okuyan dosya,
#   (c) hurda tür listesinin İKİNCİ kez elle yazılması,
#   (d) ekranın bakiyeyi kendi türetmesi (Pages/Hurda.razor içinde toplama).
# NEDEN: hurdanın stok satırı YOKTUR; bakiye yalnız defterden türetilir. Çıkış
#   kapısını atlayan tek bir yazma yolu, elde 9 has varken 400 has çıkışını
#   mümkün kılar ve has defteri EKSİYE düşer — kâr-zarar o eksiyi "kazanç"
#   sayar. Bakiyeyi ikinci bir yerde türetmek de aynı kapıyı deler: iki ekran
#   aynı soruya iki cevap verir (UX-S22).
# ÖLÇÜM KOMUTLARI (TAZE ÖLÇÜLDÜ 2026-09-21, belgeden kopyalanmadı — hepsi 0):
#   (a) awk '/^    (public|private|internal|protected|static)[^;]*\(/ {...}' Modules/Satis/SatisModulu.cs
#   (b) grep -rn 'HurdaTurleri' Kuyumcu.Api --include='*.cs' \
#         | grep -v 'Modules/Hurda/HurdaStokKapisi.cs\|Modules/Hurda/HurdaModulu.cs' | wc -l
#   (c) grep -rn '"HURDA_ALIS"[[:space:]]*,[[:space:]]*"HURDA_SATIS"' Kuyumcu.Api --include='*.cs' \
#         | grep -v 'Modules/Hurda/HurdaStokKapisi.cs' | wc -l
#   (d) grep -c 'Sum(' Kuyumcu.Web/Pages/Hurda.razor
P20_KACAK=0

# (a) kapısız yazma metodu
P20_YAZAN_DOSYA=$(grep -rl 'Tur[[:space:]]*=[[:space:]]*"HURDA_SATIS"' "$API" --include='*.cs' 2>/dev/null \
  | grep -v '/obj/\|/bin/\|/Migrations/')
while IFS= read -r f; do
  [ -n "$f" ] || continue
  P20_KACAK=$(( P20_KACAK + $(awk '
    /^    (public|private|internal|protected|static)[^;]*\(/ {
      if (acik && yaz > 0 && kapi == 0) ihlal++
      acik = 1; yaz = 0; kapi = 0
    }
    /Tur[[:space:]]*=[[:space:]]*"HURDA_SATIS"/ { yaz++ }
    /DogrulaCikis/ { kapi++ }
    END { if (acik && yaz > 0 && kapi == 0) ihlal++; print ihlal + 0 }
  ' "$f") ))
done <<< "$P20_YAZAN_DOSYA"

# (b) tür listesini kapı dışında okuyan dosya
P20_KACAK=$(( P20_KACAK + $(grep -rn 'HurdaTurleri' "$API" --include='*.cs' 2>/dev/null \
  | grep -v '/obj/\|/bin/' \
  | grep -v 'Modules/Hurda/HurdaStokKapisi\.cs\|Modules/Hurda/HurdaModulu\.cs' \
  | wc -l | tr -d ' ') ))

# (c) tür listesinin ikinci kez elle yazılması
P20_KACAK=$(( P20_KACAK + $(grep -rn '"HURDA_ALIS"[[:space:]]*,[[:space:]]*"HURDA_SATIS"' "$API" --include='*.cs' 2>/dev/null \
  | grep -v '/obj/\|/bin/' \
  | grep -v 'Modules/Hurda/HurdaStokKapisi\.cs' \
  | wc -l | tr -d ' ') ))

# (d) ekranın kendi türetmesi
if [ -f "$WEB/Pages/Hurda.razor" ]; then
  P20_KACAK=$(( P20_KACAK + $(grep -c 'Sum(' "$WEB/Pages/Hurda.razor" | tr -d ' ') ))
fi

if [ "$P20_KACAK" -eq 0 ]; then
  printf "  ✓ %-46s 0 (çıkış kapısı tek yol, bakiye tek kaynak)\n" "P-20: HURDA_STOK_KACAK_YOL"
else
  printf "  ✗ %-46s %s (hurda çıkışı kapıyı ya da tek kaynağı atlıyor)\n" \
    "P-20: HURDA_STOK_KACAK_YOL" "$P20_KACAK"; HATA=1
fi

# ── Destek kapısı: K-13 fire anahtarı KODA GÖMÜLÜ DEĞİL ────────────────────
# Fire kaydı kurucu kararına bağlıdır (K-13): kod `true` sabitiyle açılamaz,
# yalnız yapılandırmadan okunur ve varsayılanı KAPALIDIR.
P20_FIRE_VARSAYILAN=$(grep -c 'FireKaydiAcik { get; init; } = false;' \
  "$API/Modules/Hurda/GiderPusulasiKapisi.cs" 2>/dev/null || echo 0)
if [ "$P20_FIRE_VARSAYILAN" -eq 1 ]; then
  printf "  ✓ %-46s kapalı (K-13 kurucu kararı bekliyor)\n" "P-20: FIRE_VARSAYILANI"
else
  printf "  ✗ %-46s %s (fire varsayılanı KAPALI olmalı — K-13)\n" \
    "P-20: FIRE_VARSAYILANI" "$P20_FIRE_VARSAYILAN"; HATA=1
fi
# <<< P-20

# >>> P-20b — TAHLİL / MUTABAKAT FARKI (Y-10 kalan dilim · K-13)
echo
echo "═══ P-20b tahlil farkı kapısı ═══"

# ── 1) TAHLIL_FARKI_ESSIZ_BACAK = 0 (cırcır; YALNIZ AZALIR) ────────────────
# NE ÖLÇER: tahlil farkı bacağını KARŞI BACAĞINDAN (hurda satışı satırından)
#   ya da TEK İŞLEMDEN koparabilecek yolların toplamı. Üç bileşen:
#   (a) `TahlilKaynakHareketId` ATAMASININ fabrika DIŞINDA yapılması,
#   (b) "TAHLIL_FARKI" tür dizgesinin fabrika/sabit listesi DIŞINDA yazılması,
#   (c) `TahlilFarkiFabrikasi.Ekle(` çağıran metotta TAM 1 SaveChangesAsync
#       OLMAMASI (0 = hiç yazılmaz, 2+ = yarım yazma mümkün olur).
# NEDEN: fark bacağı BAĞIMSIZ bir olay değildir; daima bir HURDA_SATIS'ın
#   mutabakatıdır ve ikisinin has toplamı −(bizim kaydımız) etmek ZORUNDADIR.
#   Öksüz bir fark bacağı ya da yarım yazılan çift, hurda has bakiyesini
#   sessizce kaydırır — has-cari denk gelmez, ki bu ürünün çekirdek vaadidir.
# TABAN: 0 — TAZE ÖLÇÜLDÜ (2026-09-21), belgeden kopyalanmadı.
# ÖLÇÜM KOMUTLARI:
#   (a) grep -rnE 'TahlilKaynakHareketId[[:space:]]*=[^=]' Kuyumcu.Api --include='*.cs' \
#         | grep -v '/obj/\|/bin/\|/Migrations/' | grep -v 'Data/HareketFabrikasi.cs' | wc -l   # → 0
#   (b) grep -rn '"TAHLIL_FARKI"' Kuyumcu.Api --include='*.cs' \
#         | grep -v '/obj/\|/bin/\|/Migrations/' \
#         | grep -v 'Data/HareketFabrikasi.cs\|Data/Entities.cs' | wc -l                        # → 0
#   (c) awk '/^    (public|private|internal|protected|static)[^;]*\(/ {...}' <çağıran .cs>      # → 0
P20B_ESSIZ=0

# (a) fabrika dışı karşı-bacak ataması
P20B_ESSIZ=$(( P20B_ESSIZ + $(grep -rnE 'TahlilKaynakHareketId[[:space:]]*=[^=]' "$API" --include='*.cs' 2>/dev/null \
  | grep -v '/obj/\|/bin/\|/Migrations/' \
  | grep -v 'Data/HareketFabrikasi\.cs' | wc -l | tr -d ' ') ))

# (b) tür dizgesinin ikinci kez elle yazılması (tek kaynak: TahlilFarkiFabrikasi.Tur)
P20B_ESSIZ=$(( P20B_ESSIZ + $(grep -rn '"TAHLIL_FARKI"' "$API" --include='*.cs' 2>/dev/null \
  | grep -v '/obj/\|/bin/\|/Migrations/' \
  | grep -v 'Data/HareketFabrikasi\.cs\|Data/Entities\.cs' | wc -l | tr -d ' ') ))

# (c) fabrikayı çağıran metotta TAM 1 SaveChangesAsync
P20B_CAGIRAN=$(grep -rl 'TahlilFarkiFabrikasi\.Ekle(' "$API" --include='*.cs' 2>/dev/null \
  | grep -v '/obj/\|/bin/\|/Migrations/' | grep -v 'Data/HareketFabrikasi\.cs')
while IFS= read -r f; do
  [ -n "$f" ] || continue
  P20B_ESSIZ=$(( P20B_ESSIZ + $(awk '
    /^    (public|private|internal|protected|static)[^;]*\(/ {
      if (acik && esle > 0 && save != 1) ihlal++
      acik = 1; esle = 0; save = 0
    }
    /TahlilFarkiFabrikasi\.Ekle\(/ { esle++ }
    /SaveChangesAsync/ { save++ }
    END { if (acik && esle > 0 && save != 1) ihlal++; print ihlal + 0 }
  ' "$f") ))
done <<< "$P20B_CAGIRAN"

if [ "$P20B_ESSIZ" -eq 0 ]; then
  printf "  ✓ %-46s 0 (fark bacağı daima karşı bacağıyla, tek SaveChanges)\n" "P-20b: TAHLIL_FARKI_ESSIZ_BACAK"
else
  printf "  ✗ %-46s %s (öksüz ya da yarım yazılan tahlil farkı bacağı riski)\n" \
    "P-20b: TAHLIL_FARKI_ESSIZ_BACAK" "$P20B_ESSIZ"; HATA=1
fi

# ── 2) HURDA_FIRE_SIZINTISI = 0 (cırcır; YALNIZ AZALIR) ────────────────────
# NE ÖLÇER: K-13 kararının BİRİNCİ yarısının makine kontrolü — "eritme firesi
#   1.0'a GİRMEZ". Kodda ya da ekranda FİRE adıyla bir KAYIT YOLU açılmış mı?
#   Dört bileşen:
#   (a) adında FIRE geçen bir hareket türü ATANIYOR mu,
#   (b) Sabitler.HareketTurleri listesinde FIRE geçen tür var mı,
#   (c) ekranda adı/kimliği "fire" olan bir GİRDİ ALANI var mı
#       (açıklama paragrafı #hrd-fire-notu SAYILMAZ: o, kaydın NEDEN
#        yapılmadığını söyleyen gerekçedir — kayıt yolu değildir),
#   (d) reddetme metni K-13 gerekçesini SÖYLÜYOR mu (yoksa kullanıcı neden
#       yapamadığını bilmez — UX-S30).
# NEDEN: tahlil farkı geldiği için "madem fark yazılıyor, fireyi de yazalım"
#   baskısı doğar. İkisi AYRI kavramdır: fark bir ÖLÇÜM mutabakatsızlığı,
#   fire bir ÜRETİM kaybıdır ve imalat (Katman 1) kapsam DIŞIDIR.
# TABAN: 0 — TAZE ÖLÇÜLDÜ (2026-09-21).
# ÖLÇÜM KOMUTLARI:
#   (a) grep -rnE 'Tur[[:space:]]*=[[:space:]]*"[A-Z_]*FIRE' Kuyumcu.Api --include='*.cs' | wc -l  # → 0
#   (b) grep -oE '"[A-Z_]*FIRE[A-Z_]*"' Kuyumcu.Api/Data/Entities.cs | wc -l                       # → 0
#   (c) grep -rnE '<(input|select|textarea)[^>]*id="[^"]*[Ff]ire' Kuyumcu.Web/Pages --include='*.razor' | wc -l  # → 0
#   (d) grep -c 'imalat kapsam dışıdır' Kuyumcu.Api/Modules/Hurda/HurdaStokKapisi.cs               # → 1
P20B_FIRE=0
P20B_FIRE=$(( P20B_FIRE + $(grep -rnE 'Tur[[:space:]]*=[[:space:]]*"[A-Z_]*FIRE' "$API" --include='*.cs' 2>/dev/null \
  | grep -v '/obj/\|/bin/' | wc -l | tr -d ' ') ))
P20B_FIRE=$(( P20B_FIRE + $(grep -oE '"[A-Z_]*FIRE[A-Z_]*"' "$API/Data/Entities.cs" 2>/dev/null | wc -l | tr -d ' ') ))
P20B_FIRE=$(( P20B_FIRE + $(grep -rnE '<(input|select|textarea)[^>]*id="[^"]*[Ff]ire' "$WEB/Pages" --include='*.razor' 2>/dev/null \
  | wc -l | tr -d ' ') ))
P20B_K13=$(grep -c 'imalat kapsam dışıdır' "$API/Modules/Hurda/HurdaStokKapisi.cs" 2>/dev/null || echo 0)
[ "$P20B_K13" -ge 1 ] || P20B_FIRE=$(( P20B_FIRE + 1 ))

if [ "$P20B_FIRE" -eq 0 ]; then
  printf "  ✓ %-46s 0 (K-13/1: eritme firesi kayıt yolu AÇILMAMIŞ, gerekçe yazılı)\n" "P-20b: HURDA_FIRE_SIZINTISI"
else
  printf "  ✗ %-46s %s (fire adıyla kayıt yolu ya da eksik gerekçe — K-13/1 ihlali)\n" \
    "P-20b: HURDA_FIRE_SIZINTISI" "$P20B_FIRE"; HATA=1
fi
# <<< P-20b


# >>> P-21 — AVANS · SİPARİŞ→SATIŞ · DEĞİŞİM · KONSİNYE · CİRO/RÜCU · REZERV
echo
echo "═══ P-21 ticari çevre hatları ═══"

# ── 1) BLOKE_TUTARSIZ = 0 (cırcır; YALNIZ AZALIR) ─────────────────────────
# NE ÖLÇER: `stoklar.bloke_adet` değişmezini (0 ≤ bloke ≤ adet) delebilecek
#   yolların toplamı. Üç bileşen:
#   (a) `.BlokeAdet` ataması BlokeKapisi DIŞINDA yapılıyor mu,
#   (b) mal ÇIKARAN bir yolda HAM `stok.Adet < talep` karşılaştırması kalmış mı
#       (kapı atlanmış demektir: rezerve mal satılabilir hâle gelir),
#   (c) `BlokeKapisi.SatilabilirDogrula` çağrısı mal çıkaran BEŞ yolun
#       hepsinde duruyor mu (satış · satış fişi · transfer · alış iadesi · takas).
# NEDEN: bloke adet ADETTEN DÜŞÜLMEZ (sayım bozulmasın diye). Bu yüzden
#   "satılabilir" bir TÜRETİLMİŞ değerdir ve her çıkış yolu onu tek tek
#   sormak zorundadır. Bir yol sorgulamayı atlarsa aynı mal hem müşteriye söz
#   verilir hem satılır; hata ancak müşteri kapıya geldiğinde görünür.
#   Ters yönde: bloke > adet olursa satılabilir NEGATİF olur ve RAFTAKİ mal
#   satılamaz — ekranda "stok yok" yazar, sebebi görünmez.
# TABAN: 0 — TAZE ÖLÇÜLDÜ (2026-09-21), belgeden kopyalanmadı.
# ÖLÇÜM KOMUTLARI:
#   (a) grep -rnE '\.BlokeAdet[[:space:]]*(=|\+=|-=)[^=]' Kuyumcu.Api --include='*.cs' \
#         | grep -v '/obj/\|/bin/\|/Migrations/' | grep -v 'Data/Ticaret\.cs' | wc -l      # → 0
#   (b) grep -rnE '\.Adet[[:space:]]*<[[:space:]]*(dto|k)\.' Kuyumcu.Api/Modules --include='*.cs' | wc -l  # → 0
#   (c) grep -rn 'BlokeKapisi\.SatilabilirDogrula(' Kuyumcu.Api --include='*.cs' | wc -l   # → 5 (BEKLENEN)
P21_BLOKE_BEKLENEN_KAPI=5
P21_BLOKE=0

P21_BLOKE=$(( P21_BLOKE + $(grep -rnE '\.BlokeAdet[[:space:]]*(=|\+=|-=)[^=]' "$API" --include='*.cs' 2>/dev/null \
  | grep -v '/obj/\|/bin/\|/Migrations/' | grep -v 'Data/Ticaret\.cs' | wc -l | tr -d ' ') ))

P21_BLOKE=$(( P21_BLOKE + $(grep -rnE '\.Adet[[:space:]]*<[[:space:]]*(dto|k)\.' "$API/Modules" --include='*.cs' 2>/dev/null \
  | grep -v '/obj/\|/bin/' | wc -l | tr -d ' ') ))

P21_KAPI_SAYISI=$(grep -rn 'BlokeKapisi\.SatilabilirDogrula(' "$API" --include='*.cs' 2>/dev/null \
  | grep -v '/obj/\|/bin/' | wc -l | tr -d ' ')
[ "$P21_KAPI_SAYISI" -ge "$P21_BLOKE_BEKLENEN_KAPI" ] || P21_BLOKE=$(( P21_BLOKE + 1 ))

if [ "$P21_BLOKE" -eq 0 ]; then
  printf "  ✓ %-46s 0 (bloke yalnız BlokeKapisi'ndan yazılır, %s çıkış yolu da sorar)\n" \
    "P-21: BLOKE_TUTARSIZ" "$P21_KAPI_SAYISI"
else
  printf "  ✗ %-46s %s (kapı dışı bloke yazımı ya da atlanmış satılabilir kontrolü)\n" \
    "P-21: BLOKE_TUTARSIZ" "$P21_BLOKE"; HATA=1
fi

# ── 2) TERMINAL_SONRASI_YAZAN_YOL = 0 (cırcır; YALNIZ AZALIR) ─────────────
# NE ÖLÇER: KAPANMIŞ (terminal) bir kaydın deftere ya da stoğa YENİDEN
#   yazdırabileceği yolların toplamı. Dört bileşen:
#   (a) Rezervasyon durumunu terminale yazan her DOSYADA TerminalKapisi.Dogrula
#       çağrısı var mı,
#   (b) sipariş → satış dönüşümünde TerminalKapisi.Dogrula duruyor mu,
#   (c) `hareketler.siparis_id` çift dönüşüm kapısı (`zaten satışa çevrilmiş`)
#       duruyor mu,
#   (d) CİRO EDİLDİ durumu TERMİNAL OLMAMALI: EvrakGecisleri'nde çıkışı olmalı
#       (plan DoD: "ciro edilen evrakta rücu durumu terminal değildir").
# NEDEN: bu pakette dört ayrı durum makinesi ilk kez DEFTERE YAZAN yollar
#   kazandı. Her yol kendi `if (durum == ...)` kontrolünü yazsaydı biri
#   unutulurdu: teslim edilmiş sipariş ikinci kez satılır, iptal edilmiş
#   rezervasyon blokeyi ikinci kez düşürürdü. İkisi de SESSİZCE olur.
# TABAN: 0 — TAZE ÖLÇÜLDÜ (2026-09-21), belgeden kopyalanmadı.
# ÖLÇÜM KOMUTLARI:
#   (a) grep -rln 'Durum[[:space:]]*=[[:space:]]*RezervasyonDurumu\.' Kuyumcu.Api --include='*.cs'
#       → her dosyada: grep -c 'TerminalKapisi\.Dogrula(' ≥ 1
#   (b) grep -c 'TerminalKapisi\.Dogrula(.*SiparisDurumu\.Terminal' Kuyumcu.Api/Modules/Satis/SatisModulu.P21.cs  # → ≥ 1
#   (c) grep -c 'zaten satışa çevrilmiş' Kuyumcu.Api/Modules/Satis/SatisModulu.P21.cs                            # → 1
#   (d) grep -c 'CiroEdildi\] = \[EvrakDurumu\.' Kuyumcu.Api/Data/KiymetliEvrak.cs                               # → 1
P21_TERMINAL=0

# (a) rezervasyon durumunu yazan her dosya kapıyı çağırmalı
P21_REZ_DOSYA=$(grep -rln 'Durum[[:space:]]*=[[:space:]]*RezervasyonDurumu\.' "$API" --include='*.cs' 2>/dev/null \
  | grep -v '/obj/\|/bin/')
while IFS= read -r f; do
  [ -n "$f" ] || continue
  if [ "$(grep -c 'TerminalKapisi\.Dogrula(' "$f" 2>/dev/null)" -lt 1 ]; then
    P21_TERMINAL=$(( P21_TERMINAL + 1 ))
  fi
done <<< "$P21_REZ_DOSYA"

# (b) sipariş dönüşümünde terminal kapısı
P21_DONUSUM="$API/Modules/Satis/SatisModulu.P21.cs"
[ "$(grep -c 'TerminalKapisi\.Dogrula(.*SiparisDurumu\.Terminal' "$P21_DONUSUM" 2>/dev/null)" -ge 1 ] \
  || P21_TERMINAL=$(( P21_TERMINAL + 1 ))

# (c) çift dönüşüm kapısı (siparis_id bağı)
[ "$(grep -c 'zaten satışa çevrilmiş' "$P21_DONUSUM" 2>/dev/null)" -ge 1 ] \
  || P21_TERMINAL=$(( P21_TERMINAL + 1 ))

# (d) CIRO_EDILDI'nin çıkışı OLMALI — yoksa rücu imkânsız olur
[ "$(grep -cE 'CiroEdildi\] = \[EvrakDurumu\.' "$API/Data/KiymetliEvrak.cs" 2>/dev/null)" -ge 1 ] \
  || P21_TERMINAL=$(( P21_TERMINAL + 1 ))

if [ "$P21_TERMINAL" -eq 0 ]; then
  printf "  ✓ %-46s 0 (kapanmış kayıt deftere yazdıramaz; ciro terminal DEĞİL)\n" \
    "P-21: TERMINAL_SONRASI_YAZAN_YOL"
else
  printf "  ✗ %-46s %s (terminal kapısı eksik ya da ciro terminale kapatılmış)\n" \
    "P-21: TERMINAL_SONRASI_YAZAN_YOL" "$P21_TERMINAL"; HATA=1
fi
# <<< P-21


# >>> P-22 — SATIŞ / FATURA / SİPARİŞ / SAYIM / POS EKRAN BORÇLARI
# Kimlikler: B-3 · B-26/K-23 · B-33 · B-44 · B-45 · B-46 · B-55 · B-56
echo
echo "═══ P-22 belge serisi ve durum sütunu kapısı ═══"

# ── SERI_TUKETIM_NOKTASI = 1 ────────────────────────────────────────────────
# Fatura numarası serisini tüketen TEK yer FaturaSeriKapisi.Tuket olmalıdır.
# Taslak belge numara almaz; numara yalnız belge KESİLDİĞİNDE verilir (B-3).
# ÖLÇÜM: grep -rn 'FaturaNoUretici\.Siradaki(' Kuyumcu.Api --include='*.cs' \
#          | grep -v '/obj/\|/bin/' | grep -v 'SiradakiPusula' | wc -l
# TABAN (2026-09-21, TAZE ÖLÇÜLDÜ): 1  ← ARTIRILAMAZ, yalnız azalır.
P22_SERI=$(grep -rn 'FaturaNoUretici\.Siradaki(' "$API" --include='*.cs' 2>/dev/null \
  | grep -v '/obj/\|/bin/' | grep -v 'SiradakiPusula' | wc -l | tr -d ' ')
if [ "$P22_SERI" -le 1 ]; then
  printf "  ✓ %-46s %s (tek nokta: FaturaSeriKapisi.Tuket)\n" "P-22: SERI_TUKETIM_NOKTASI" "$P22_SERI"
else
  printf "  ✗ %-46s %s (en çok 1 olmalı)\n" "P-22: SERI_TUKETIM_NOKTASI" "$P22_SERI"
  grep -rn 'FaturaNoUretici\.Siradaki(' "$API" --include='*.cs' 2>/dev/null \
    | grep -v '/obj/\|/bin/' | grep -v 'SiradakiPusula' | sed "s|.*/$API/|      → |" | cut -c1-100
  HATA=1
fi

# ── TASLAK_NUMARA_SIZINTISI = 0 ────────────────────────────────────────────
# Belge OLUŞTURULURKEN numara atanması yasaktır: 'Durum ... "TASLAK"' yazan
# blokta FaturaNo bir değişkene bağlanıyorsa seri taslakta yakılıyor demektir.
# ⚠ KAPSAM: yalnız BELGE modülleri (Fatura + Banka). `hareketler.fatura_no`
#   kolonu HURDA_ALIS satırlarında TEDARİKÇİNİN numarasını taşır (Satis modülü)
#   ve bizim serimizle ilgisi yoktur; ona bakılsaydı sayaç yalan söylerdi.
# ÖLÇÜM: grep -rnE 'FaturaNo +=' Kuyumcu.Api/Modules/Fatura Kuyumcu.Api/Modules/Banka \
#          --include='*.cs' | grep -vE 'FaturaNo *= *null' | grep -v 'FaturaSeriKapisi' | wc -l
# TABAN (2026-09-21, TAZE ÖLÇÜLDÜ): 0
P22_TASLAK_NO=$(grep -rnE 'FaturaNo +=' "$API/Modules/Fatura" "$API/Modules/Banka" --include='*.cs' 2>/dev/null \
  | grep -v '/obj/\|/bin/' \
  | grep -vE 'FaturaNo *= *null' \
  | grep -v 'FaturaSeriKapisi' \
  | grep -vE ':[0-9]+:[[:space:]]*(//|///|\*)' | wc -l | tr -d ' ')
if [ "$P22_TASLAK_NO" = 0 ]; then
  printf "  ✓ %-46s 0 (numara yalnız kesimde yazılır)\n" "P-22: TASLAK_NUMARA_SIZINTISI"
else
  printf "  ✗ %-46s %s (0 olmalı)\n" "P-22: TASLAK_NUMARA_SIZINTISI" "$P22_TASLAK_NO"
  grep -rnE 'FaturaNo +=' "$API/Modules/Fatura" "$API/Modules/Banka" --include='*.cs' 2>/dev/null \
    | grep -vE 'FaturaNo *= *null' | grep -v 'FaturaSeriKapisi' \
    | sed "s|.*/$API/|      → |" | cut -c1-100
  HATA=1
fi

# ── DURUM_SUTUNU_EYLEM_METNI = 0 ───────────────────────────────────────────
# DURUM sütunu GERÇEK durumu yazar; eylem ("İptal Et", "Kes") ayrı sütundadır.
# Ölçülebilir kural: id'si '-durum-' taşıyan tablo hücresinde tıklanabilir
# eylem BULUNMAZ (B-33).
# ÖLÇÜM: grep -rn -- '-durum-' Kuyumcu.Web/Pages --include='*.razor' \
#          | grep '<td' | grep -c '@onclick'
# TABAN (2026-09-21, TAZE ÖLÇÜLDÜ): 0
P22_DURUM=$(grep -rn -- '-durum-' "$WEB/Pages" --include='*.razor' 2>/dev/null \
  | grep '<td' | grep -c '@onclick' | tr -d ' ')
if [ "$P22_DURUM" = 0 ]; then
  printf "  ✓ %-46s 0 (durum sütununda eylem yok)\n" "P-22: DURUM_SUTUNU_EYLEM_METNI"
else
  printf "  ✗ %-46s %s (0 olmalı)\n" "P-22: DURUM_SUTUNU_EYLEM_METNI" "$P22_DURUM"
  HATA=1
fi
# <<< P-22

# >>> P-36 — GÖRSEL DİL + KULLANICI DOSTU EŞİĞİ (frontend şeridi, 2026-09-21)
# Kimlikler: kurucu talimatı 19 (tek ortak dropdown) · talimat 20/a (ortak girdiler)
#            FRONTEND-SARTNAME § 7 (mobil taşma) · FRONTEND-POPUP (modal kaydırma)
# ⚠ TÜM TABANLAR 2026-09-21'de TAZE ÖLÇÜLDÜ (belgeden kopyalanmadı).
echo
echo "═══ P-36 görsel dil / ortak bileşen cırcırı ═══"

# ── HAM_SELECT (cırcır; YALNIZ AZALIR) ─────────────────────────────────────
# Kurucu: "projede ne kadar dropdown varsa hepsi aynı olmalı… bir tane bize özel
# şık dropdown yapıp hepsinde onu kullanmalıyız ki tek seferde değiştiririz."
# Yerleşik <select>'in AÇILIR LİSTESİ CSS ile biçimlendirilemez (işletim sistemi
# çizer) → tek çözüm ortak <Secici> bileşenidir; ham select sayısı 0'a iner.
# ÖLÇÜM: grep -rho '<select' Kuyumcu.Web/Pages Kuyumcu.Web/Bilesenler Kuyumcu.Web/Layout \
#          --include='*.razor' --exclude='Secici.razor' | wc -l
#   ⚠ Secici.razor DIŞLANIR: ortak bileşenin KENDİSİ hedeftir, yorumunda geçen
#     "<select>" sözcüğü sayaca girerse sayaç yalan söyler.
# TABAN (2026-09-21, AYNI KOMUTLA TAZE ÖLÇÜLDÜ):
#   paket ÖNCESİ 178  →  paket SONRASI 176 (pilot göç: SubeSecici + Panel).
#   ⚠ Kaba '<select' araması 176 veriyordu; '<select[ >]' + Secici.razor hariç
#     tutulunca 178 çıktı. Rapora YANLIŞ olan değil, ÖLÇÜLEN yazıldı (md.1).
# HEDEF: 0
#   P-36c (2026-09-21): 92 select ortak <Secici>'ye GÖÇTÜ → TAVAN 176'dan
#   **84**'e indirildi. Kalan 84'ün 26'sı KİLİTLİ ekranlarda (Faturalar,
#   KarZarar, KasaRaporu, Kilavuz, Pos, Raporlar, SatisAnalizi, Sayim,
#   Siparisler — o paketlere devredildi), 58'i serbest ekranlarda elle
#   dönüştürülecek kalıntıdır (rapor § Devredilenler). Tavan ARTIRILMAZ.
#   P-36c/2 (2026-09-21, ikinci tur): serbest ekranlardaki 58 select de göçtü.
#   TAZE ÖLÇÜM: 30 (26'sı kilitli ekranlarda GERÇEK <select>, 4'ü yorum metni).
#   TAVAN 84 → **30**. Kilitli ekranlar kendi paketlerinde 0'a inecek.
P36_SELECT_TAVAN=30
P36_SELECT=$(grep -rho '<select[ >]' Kuyumcu.Web/Pages Kuyumcu.Web/Bilesenler Kuyumcu.Web/Layout \
  --include='*.razor' --exclude='Secici.razor' 2>/dev/null | wc -l | tr -d ' ')
if [ "$P36_SELECT" -le "$P36_SELECT_TAVAN" ]; then
  printf "  ✓ %-46s %s (tavan %s, yalnız azalır)\n" "P-36: HAM_SELECT" "$P36_SELECT" "$P36_SELECT_TAVAN"
else
  printf "  ✗ %-46s %s > tavan %s — ham <select> EKLENDİ, ortak Secici kullanılmalı\n" \
    "P-36: HAM_SELECT" "$P36_SELECT" "$P36_SELECT_TAVAN"; HATA=1
fi

# ── HAM_INPUT (cırcır; YALNIZ AZALIR) ──────────────────────────────────────
# Kurucu talimatı 20/a: "tüm inputlar için bize ait birer tane component olmalı".
# ÖLÇÜM: grep -rhoE '<(input|textarea)' Kuyumcu.Web/Pages Kuyumcu.Web/Bilesenler \
#          Kuyumcu.Web/Layout --include='*.razor' | wc -l
# TABAN (2026-09-21, TAZE): 365 → P-36b'de **357**'ye DÜŞTÜ (pilot göç:
#   /ayarlar profil 3 alan + /isletme kimlik 5 alan = 8 ham girdi devredildi).
#   ⚠ Bilesenler/Form/ ÖLÇÜMÜN DIŞINDADIR: Secici.razor'un dışlanmasıyla aynı
#     gerekçe — ortak bileşen ailesinin KENDİSİ hedeftir, kendi <input>'u
#     sayaca girerse sayaç yalan söyler (aile büyüdükçe "ham girdi arttı"
#     görünürdü).
# 2026-09-21 (P-36c/3, TAZE): 357 → **42**. Serbest ekranların TAMAMI ortak
# girdi ailesine geçti; kalan 42'nin 39'u KİLİTLİ ekranlarda (Faturalar, Pos,
# Siparisler, Sayim, KasaRaporu, SatisAnalizi, KarZarar, Raporlar), 3'ü ortak
# girdi katmanındadır (KayitSecici · SatirIciDuzenle · GlobalArama).
P36_INPUT_TAVAN=42
P36_INPUT=$(grep -rhoE '<(input|textarea)' Kuyumcu.Web/Pages Kuyumcu.Web/Bilesenler \
  Kuyumcu.Web/Layout --include='*.razor' --exclude='Secici.razor' \
  --exclude-dir=Form 2>/dev/null | wc -l | tr -d ' ')
if [ "$P36_INPUT" -le "$P36_INPUT_TAVAN" ]; then
  printf "  ✓ %-46s %s (tavan %s, yalnız azalır)\n" "P-36: HAM_INPUT" "$P36_INPUT" "$P36_INPUT_TAVAN"
else
  printf "  ✗ %-46s %s > tavan %s — ortak Form bileşeni kullanılmalı\n" \
    "P-36: HAM_INPUT" "$P36_INPUT" "$P36_INPUT_TAVAN"; HATA=1
fi

# ── MODAL_GOVDE_DISI_KAYDIRMA = 0 ──────────────────────────────────────────
# Ölçülmüş kök neden (FRONTEND-POPUP-DENETIMI): `.modal-panel` kaydırma kabının
# KENDİSİYDİ → başlık ve Kaydet/İptal içerikle birlikte kayıyordu (Yeni Ürün
# @375: +2029px taşma). Doğru desen Cekmece'de zaten vardı. Kural: kaydırma
# YALNIZ gövdede; `.modal-govde` overflow:auto, `.modal-panel` overflow:hidden.
# ÖLÇÜM: (a) grep -c 'modal-govde { flex: 1 1 auto; overflow: auto' app.css  # → 1
#        (b) app.css'te SON `.modal-panel` bloğunda 'overflow: hidden' bulunmalı
P36_CSS=Kuyumcu.Web/wwwroot/css/app.css
P36_GOVDE=$(grep -c 'modal-govde { flex: 1 1 auto; overflow: auto' "$P36_CSS" 2>/dev/null | tr -d ' ')
P36_PANEL=$(grep -c 'overflow: hidden;              /\* ← kaydırma kabı ARTIK panel DEĞİL \*/' "$P36_CSS" 2>/dev/null | tr -d ' ')
if [ "$P36_GOVDE" = 1 ] && [ "$P36_PANEL" = 1 ]; then
  printf "  ✓ %-46s 0 (kaydırma yalnız gövdede)\n" "P-36: MODAL_GOVDE_DISI_KAYDIRMA"
else
  printf "  ✗ %-46s govde=%s panel=%s (ikisi de 1 olmalı)\n" \
    "P-36: MODAL_GOVDE_DISI_KAYDIRMA" "$P36_GOVDE" "$P36_PANEL"; HATA=1
fi

# ── MOBIL_SATIR_TASMASI = 0 ────────────────────────────────────────────────
# FRONTEND-SARTNAME § 7: 375px'te 6 ekranda yatay taşma vardı; BEŞİ `.satir` ve
# `.satir-arasi` flex-wrap taşımamasından. ⚠ Taşma `document`ta değil
# `main.ana-icerik` içinde birikir — `documentElement.scrollWidth` YANILTICI.
# ÖLÇÜM: grep -c '^\.satir       { flex-wrap: wrap; min-width: 0; }' app.css   # → 1
#        grep -c '^\.satir-arasi { flex-wrap: wrap; min-width: 0; }' app.css   # → 1
#        grep -c '^\.sekme-serit { min-width: 0; max-width: 100%; }' app.css   # → 1
P36_SATIR=$(grep -cE '^\.satir(-arasi)? +\{ flex-wrap: wrap; min-width: 0; \}' "$P36_CSS" 2>/dev/null | tr -d ' ')
P36_SEKME=$(grep -c '^\.sekme-serit { min-width: 0; max-width: 100%; }' "$P36_CSS" 2>/dev/null | tr -d ' ')
if [ "$P36_SATIR" = 2 ] && [ "$P36_SEKME" = 1 ]; then
  printf "  ✓ %-46s 0 (satır yardımcıları sarıyor)\n" "P-36: MOBIL_SATIR_TASMASI"
else
  printf "  ✗ %-46s satir=%s sekme=%s (2 ve 1 olmalı)\n" \
    "P-36: MOBIL_SATIR_TASMASI" "$P36_SATIR" "$P36_SEKME"; HATA=1
fi

# ── KONTRAST_IHLALI = 0 ────────────────────────────────────────────────────
# FRONTEND-KIMLIK § 1.3: TEK gerçek kontrast ihlali Panel.razor'daki
# `.btn-birincil` + `.dugme-benzeri` birleşimiydi (1.60:1). `.dugme-benzeri`
# color:var(--metin) yazıp birincil düğmenin beyaz metnini eziyordu; sınıf
# silinince oran 10.17:1. Kural: iki sınıf AYNI elemanda BULUNMAZ.
# ÖLÇÜM: grep -rn 'btn-birincil' Kuyumcu.Web/Pages Kuyumcu.Web/Bilesenler \
#          Kuyumcu.Web/Layout --include='*.razor' | grep -c 'dugme-benzeri'
# TABAN (2026-09-21 paket ÖNCESİ, TAZE): 1  →  paket SONRASI: 0
P36_KONTRAST=$(grep -rn 'btn-birincil' Kuyumcu.Web/Pages Kuyumcu.Web/Bilesenler \
  Kuyumcu.Web/Layout --include='*.razor' 2>/dev/null | grep -c 'dugme-benzeri' | tr -d ' ')
if [ "$P36_KONTRAST" = 0 ]; then
  printf "  ✓ %-46s 0\n" "P-36: KONTRAST_IHLALI"
else
  printf "  ✗ %-46s %s (0 olmalı — birincil düğme okunmuyor)\n" \
    "P-36: KONTRAST_IHLALI" "$P36_KONTRAST"; HATA=1
fi

# ── SECICI_TEK_KAYNAK = 1 ──────────────────────────────────────────────────
# Kurucunun "tek seferde değiştiririz" şartı ancak TEK bileşen + TEK görsel
# kaynak ile tutar: ikinci bir dropdown bileşeni AÇILMAZ, Secici içinde satır
# içi renk/px YAZILMAZ (değerler belirtecler.css'ten gelir).
# ÖLÇÜM: ls Kuyumcu.Web/Bilesenler/Secici.razor | wc -l                      # → 1
#        grep -cE 'style="[^"]*(#|px|rgb)' Kuyumcu.Web/Bilesenler/Secici.razor # → 0
P36_SECICI=$([ -f Kuyumcu.Web/Bilesenler/Secici.razor ] && echo 1 || echo 0)
P36_SATIRICI=$(grep -cE 'style="[^"]*(#|px|rgb)' Kuyumcu.Web/Bilesenler/Secici.razor 2>/dev/null | tr -d ' ')
if [ "$P36_SECICI" = 1 ] && [ "$P36_SATIRICI" = 0 ]; then
  printf "  ✓ %-46s 1 (satır içi renk/px: 0)\n" "P-36: SECICI_TEK_KAYNAK"
else
  printf "  ✗ %-46s bilesen=%s satirici=%s\n" \
    "P-36: SECICI_TEK_KAYNAK" "$P36_SECICI" "$P36_SATIRICI"; HATA=1
fi
# <<< P-36

# >>> P-36c — GÖÇ + PENCERE TEK TİPİ (2026-09-21)
# Kimlikler: kurucu talimatı 19 (tek dropdown) · FRONTEND-POPUP (odak/kaydırma)
# ⚠ TABANLAR 2026-09-21'de TAZE ÖLÇÜLDÜ.
echo
echo "═══ P-36c göç + pencere cırcırı ═══"

# ── SECICI_GOC (cırcır; YALNIZ ARTAR) ──────────────────────────────────────
# Göçün ilerlemesi geri alınamaz: ortak bileşene geçmiş bir ekran ham
# <select>'e DÖNEMEZ. HAM_SELECT tavanıyla birlikte iki yönlü kilit kurar.
# TABAN (2026-09-21): P-36 pilotu 2 → P-36c sonrası 92.
# ⚠ Doğrulayıcı bulgusu (2026-09-21): taban 92 iken ÖLÇÜLEN 97'ydi; 5 birimlik
#   geri dönüş boşluğu vardı. Cırcır ÖLÇÜLEN SON DURUMA kilitlenir.
#   P-36c/2 TAZE ÖLÇÜM: 155.
P36C_GOC_TABAN=155
P36C_GOC=$(grep -rho '<Secici[ >]' Kuyumcu.Web/Pages Kuyumcu.Web/Bilesenler Kuyumcu.Web/Layout \
  --include='*.razor' 2>/dev/null | wc -l | tr -d ' ')
if [ "$P36C_GOC" -ge "$P36C_GOC_TABAN" ]; then
  printf "  ✓ %-46s %s (taban %s, yalnız artar)\n" "P-36c: SECICI_GOC" "$P36C_GOC" "$P36C_GOC_TABAN"
else
  printf "  ✗ %-46s %s < taban %s — göç GERİ ALINMIŞ\n" \
    "P-36c: SECICI_GOC" "$P36C_GOC" "$P36C_GOC_TABAN"; HATA=1
fi

# ── ODAK_TUZAGI = 0 ────────────────────────────────────────────────────────
# ÖLÇÜLEN HATA: `Acik=false` olunca panel @if bloğuyla DOM'dan çıkıyor,
# `kuyumcuCekmece.kapat` ANCAK ONDAN SONRA kopmuş ElementReference ile
# çağrılıyordu → odak tetikleyiciye HİÇ dönmüyordu; üstelik hata `catch { }`
# ile yutuluyordu (Anayasa md.1 ihlali). Kural: kapatma yolu TEK (`KapatIstegi`),
# odak geri verme ONDAN ÖNCE, yutma YOK.
# ÖLÇÜM: her iki bileşende (a) `await OdakGeriVer();` VAR,
#        (b) `Kapat.InvokeAsync()` TAM 1 kez (ikinci kapatma yolu açılmamış),
#        (c) OdakGeriVer gövdesinde `catch { ` YOK.
P36C_ODAK=0
for D in ModalPencere Cekmece; do
  F="Kuyumcu.Web/Bilesenler/$D.razor"
  grep -q 'await OdakGeriVer();' "$F" || P36C_ODAK=$((P36C_ODAK+1))
  N=$(grep -c 'Kapat\.InvokeAsync()' "$F" | tr -d ' ')
  [ "$N" = 1 ] || P36C_ODAK=$((P36C_ODAK+1))
  sed -n '/private async Task OdakGeriVer()/,/^    }/p' "$F" | grep -q 'catch (JSDisconnectedException)' \
    || P36C_ODAK=$((P36C_ODAK+1))
  sed -n '/private async Task OdakGeriVer()/,/^    }/p' "$F" | grep -q 'catch { ' \
    && P36C_ODAK=$((P36C_ODAK+1))
done
if [ "$P36C_ODAK" = 0 ]; then
  printf "  ✓ %-46s 0 (odak tetikleyiciye döner, yutma yok)\n" "P-36c: ODAK_TUZAGI"
else
  printf "  ✗ %-46s %s ihlal (ModalPencere/Cekmece)\n" "P-36c: ODAK_TUZAGI" "$P36C_ODAK"; HATA=1
fi

# ── PENCERE_KAYDIRMA_KILIDI = 1 (tek yer, sayaçlı) ─────────────────────────
# Pencere açıkken arka sayfa kayıyordu. Kilit TEK yerde (cekmece.js) ve
# SAYAÇLIDIR: iç içe/ardışık iki panel birbirinin kilidini açmaz.
P36C_KILIT=0
grep -q '__kuyumcuPencereSayisi' Kuyumcu.Web/wwwroot/js/cekmece.js || P36C_KILIT=1
grep -q "body.pencere-acik { overflow-y: hidden; }" Kuyumcu.Web/wwwroot/css/app.css || P36C_KILIT=1
if [ "$P36C_KILIT" = 0 ]; then
  printf "  ✓ %-46s 1 (tek yer, sayaçlı)\n" "P-36c: PENCERE_KAYDIRMA_KILIDI"
else
  printf "  ✗ %-46s eksik (cekmece.js + app.css)\n" "P-36c: PENCERE_KAYDIRMA_KILIDI"; HATA=1
fi

# ── SECICI_TEST_TEK_KAPI = 1 ───────────────────────────────────────────────
# Testler seçimi TEK yardımcıdan yapar (SeciciYardim); ikinci "seçici tıklama"
# kalıbı açılmaz — yoksa göçün geri kalanı her dosyada yeniden icat edilir.
P36C_YARD=$([ -f Kuyumcu.Tests/SeciciYardim.cs ] && echo 1 || echo 0)
if [ "$P36C_YARD" = 1 ]; then
  printf "  ✓ %-46s 1\n" "P-36c: SECICI_TEST_TEK_KAPI"
else
  printf "  ✗ %-46s 0 (SeciciYardim.cs yok)\n" "P-36c: SECICI_TEST_TEK_KAPI"; HATA=1
fi

# ══════════════════════════════════════════════════════════════════════════
# P-36c/2 (2026-09-21, ikinci tur) — GÖÇÜN BİTİŞ SAYAÇLARI
# Doğrulayıcı bulgusu: "HAM_SELECT → 0, PENCERE_* ve BOS_DURUM_* sayaçları
# denetim.sh'te HİÇ YOK". Açılıyorlar. TABANLAR BELGEDEN DEĞİL, ÖLÇÜMDEN.
# ══════════════════════════════════════════════════════════════════════════

# ── HAM_SELECT_SERBEST = 0 (fail-closed) ───────────────────────────────────
# SERBEST ekranlarda ham <select> KALMAZ. Kilitli ekranlar (Faturalar, KarZarar,
# KasaRaporu, Kilavuz, Pos, Raporlar, SatisAnalizi, Sayim, Siparisler) bu paketin
# kapsamı DIŞINDADIR ve kendi paketlerine devredilmiştir — onlar P36_SELECT
# tavanıyla ölçülmeye devam eder.
# ⚠ YORUMLAR AYIKLANIR: Razor (@* *@) ve C# (//) yorumlarında geçen "<select>"
#   sözcüğü bir DENETİM ÖĞESİ DEĞİLDİR; sayaca girerse sayaç yalan söyler.
#   (Yorum metnini sayaçtan kaçmak için değiştirmek de yalan olurdu — md.1.)
P36C_KILITLI_EKRAN='Faturalar|KarZarar|KasaRaporu|Kilavuz|Pos|Raporlar|SatisAnalizi|Sayim|Siparisler'
P36C_SERBEST_SELECT=0
for _f in $(ls Kuyumcu.Web/Pages/*.razor Kuyumcu.Web/Bilesenler/*.razor Kuyumcu.Web/Layout/*.razor 2>/dev/null \
            | grep -vE "/($P36C_KILITLI_EKRAN)\.razor$"); do
  _n=$(perl -0777 -ne 's/\@\*.*?\*\@//gs; s{^\s*//.*$}{}gm; $n=0; $n++ while /<select[ >]/g; print $n' -- "$_f")
  P36C_SERBEST_SELECT=$((P36C_SERBEST_SELECT + _n))
done
if [ "$P36C_SERBEST_SELECT" = 0 ]; then
  printf "  ✓ %-46s 0 (serbest ekranlarda ham select yok)\n" "P-36c: HAM_SELECT_SERBEST"
else
  printf "  ✗ %-46s %s — serbest ekranda ham <select>; ortak Secici kullanılmalı\n" \
    "P-36c: HAM_SELECT_SERBEST" "$P36C_SERBEST_SELECT"; HATA=1
fi

# ── PENCERE_DUGME_SIRASI_TERS = 0 ──────────────────────────────────────────
# ÖLÇÜLDÜ (2026-09-21): ModalPencere kullanan pencerelerin 20'si "İptal → Kaydet",
# Çekmece ve 4 pencere tersiydi. İki farklı sıra kas hafızasını bozar.
# STANDART (ölçülen çoğunluk): İKİNCİL ÖNCE, BİRİNCİL SONDA.
# Ölçüm: her ModalPencere bloğu ve her .cekmece-alt altbilgisi içinde ilk
# birincil düğme ilk ikincilden ÖNCE geliyorsa ihlaldir.
P36C_TERS=$(perl -0777 -ne '
  my @b;
  push @b, $1 while /(<ModalPencere\b.*?<\/ModalPencere>)/gs;
  push @b, $1 while /(<footer class="cekmece-alt">.*?<\/footer>)/gs;
  for my $blok (@b) {
    my $d = "";
    while ($blok =~ /<button[^>]*class="[^"]*(?:btn|dugme)-(birincil|ikincil)[^"]*"/gs) {
      $d .= ($1 eq "birincil" ? "B" : "I");
    }
    next unless $d =~ /B/ && $d =~ /I/;
    $say++ if index($d, "B") < index($d, "I");
  }
  END { print $say + 0 }
' Kuyumcu.Web/Pages/*.razor Kuyumcu.Web/Bilesenler/*.razor 2>/dev/null)
if [ "$P36C_TERS" = 0 ]; then
  printf "  ✓ %-46s 0 (ikincil önce, birincil sonda)\n" "P-36c: PENCERE_DUGME_SIRASI_TERS"
else
  printf "  ✗ %-46s %s pencere ters sırada — tek tip düğme sırası bozuldu\n" \
    "P-36c: PENCERE_DUGME_SIRASI_TERS" "$P36C_TERS"; HATA=1
fi

# ── PENCERE_GENISLIK_CESIDI <= 3 ───────────────────────────────────────────
# Pencere genişliği TEK YERDE ve SAYILI olsun: `.modal-panel` ve `.cekmece`
# seçicilerindeki (max-)width bildirimlerinin FARKLI değerleri sayılır.
# Tam ekran mobil geri dönüşleri (none / 100vw) boy DEĞİLDİR, dışlanır.
# ⚠ `.cekmece-kapat` gibi ALT ÖĞELER pencere DEĞİLDİR; (?![\w-]) ile dışlanır.
# ÖLÇÜM (2026-09-21): 2 boy (modal 32rem · çekmece min(30rem,100vw)). TAVAN 3.
P36C_GENISLIK_TAVAN=3
P36C_GENISLIK=$(perl -0777 -ne '
  my %g;
  while (/(\.(?:modal-panel|cekmece)(?![\w-])[^{]*)\{([^}]*)\}/gs) {
    my $govde = $2;
    while ($govde =~ /(?:max-)?width\s*:\s*([^;]+)/g) {
      my $v = $1; $v =~ s/^\s+|\s+$//g;
      next if $v eq "none" || $v eq "100vw" || $v eq "100%" || $v eq "0";
      $g{$v} = 1;
    }
  }
  END { print scalar(keys %g) }
' Kuyumcu.Web/wwwroot/css/app.css)
if [ "$P36C_GENISLIK" -le "$P36C_GENISLIK_TAVAN" ]; then
  printf "  ✓ %-46s %s (tavan %s)\n" "P-36c: PENCERE_GENISLIK_CESIDI" "$P36C_GENISLIK" "$P36C_GENISLIK_TAVAN"
else
  printf "  ✗ %-46s %s > tavan %s — pencere boyu çoğaldı, 3 boya inmeli\n" \
    "P-36c: PENCERE_GENISLIK_CESIDI" "$P36C_GENISLIK" "$P36C_GENISLIK_TAVAN"; HATA=1
fi

# ── BOS_DURUM_METINSIZ = 0 ─────────────────────────────────────────────────
# UX sözleşmesi: boş durum ZORUNLU ve METİNLİ. Mesajsız <BosDurum /> "kayıt yok"
# bile demez; kullanıcı ekranın yüklenmediğini sanır.
P36C_BOSDURUM=$(grep -rhoE '<BosDurum[^>]*>' Kuyumcu.Web/Pages Kuyumcu.Web/Bilesenler \
  --include='*.razor' 2>/dev/null | grep -vcE 'Mesaj=' || true)
if [ "$P36C_BOSDURUM" = 0 ]; then
  printf "  ✓ %-46s 0 (her boş durum metinli)\n" "P-36c: BOS_DURUM_METINSIZ"
else
  printf "  ✗ %-46s %s metinsiz <BosDurum>\n" "P-36c: BOS_DURUM_METINSIZ" "$P36C_BOSDURUM"; HATA=1
fi

# ══════════════════════════════════════════════════════════════════════════
# P-36c/3 (2026-09-21, üçüncü tur) — HAM GİRDİ GÖÇÜNÜN BİTİŞ SAYAÇLARI
# Doğrulayıcı bulgusu: "paketin 2. ayağı (ham girdi göçü) hiç yapılmadı —
# HAM_INPUT 357'de duruyor". Göç yapıldı; bitiş fail-closed ÖLÇÜLÜYOR.
# ══════════════════════════════════════════════════════════════════════════

# ── HAM_INPUT_SERBEST = 0 (fail-closed) ────────────────────────────────────
# SERBEST ekranlarda ham <input>/<textarea> KALMAZ. Kilitli ekranlar
# (Faturalar, KarZarar, KasaRaporu, Kilavuz, Pos, Raporlar, SatisAnalizi,
# Sayim, Siparisler) bu paketin kapsamı DIŞINDADIR ve kendi paketlerine
# devredilmiştir; onlar P36_INPUT tavanıyla ölçülmeye devam eder.
# ⚠ YORUMLAR AYIKLANIR: Razor (@* *@) ve C# (//) yorumlarında geçen "<input>"
#   sözcüğü denetim öğesi DEĞİLDİR (HAM_SELECT_SERBEST ile aynı gerekçe).
# ⚠ ORTAK GİRDİ KATMANI ölçümün dışındadır — kendisi hedeftir:
#   Bilesenler/Form/* · Secici · KayitSecici · SatirIciDuzenle · GlobalArama.
P36C3_ISTISNA='Secici|KayitSecici|SatirIciDuzenle|GlobalArama'
P36C3_SERBEST_INPUT=0
for _f in $(ls "$WEB"/Pages/*.razor "$WEB"/Bilesenler/*.razor "$WEB"/Layout/*.razor 2>/dev/null \
            | grep -vE "/($P36C_KILITLI_EKRAN)\.razor$" \
            | grep -vE "/($P36C3_ISTISNA)\.razor$"); do
  _n=$(perl -0777 -ne 's/\@\*.*?\*\@//gs; s{^\s*//.*$}{}gm; $n=0; $n++ while /<(input|textarea)[ >]/g; print $n' -- "$_f")
  P36C3_SERBEST_INPUT=$((P36C3_SERBEST_INPUT + _n))
done
if [ "$P36C3_SERBEST_INPUT" = 0 ]; then
  printf "  ✓ %-46s 0 (serbest ekranlarda ham girdi yok)\n" "P-36c: HAM_INPUT_SERBEST"
else
  printf "  ✗ %-46s %s — serbest ekranda ham <input>; ortak Form bileşeni kullanılmalı\n" \
    "P-36c: HAM_INPUT_SERBEST" "$P36C3_SERBEST_INPUT"; HATA=1
fi

# ── ORTAK_GIRDI_ISTISNA (cırcır; YALNIZ AZALIR) ────────────────────────────
# Ortak girdi katmanında HÂLÂ ham <input> taşıyan üç bileşen: KayitSecici
# (uzak arama), SatirIciDuzenle (ElementReference ile odak) ve GlobalArama
# (klavye gezinme + kısayol). Üçü de aileye alınabilir ama davranışları
# (odak/klavye) ayrı bir dilim ister — SAYIYLA beyan edilir, gizlenmez.
P36C3_ISTISNA_N=0
for _f in KayitSecici SatirIciDuzenle GlobalArama; do
  _n=$(perl -0777 -ne 's/\@\*.*?\*\@//gs; $n=0; $n++ while /<(input|textarea)[ >]/g; print $n' -- "$WEB/Bilesenler/$_f.razor" 2>/dev/null)
  P36C3_ISTISNA_N=$((P36C3_ISTISNA_N + ${_n:-0}))
done
if [ "$P36C3_ISTISNA_N" -le 3 ]; then
  printf "  ✓ %-46s %s (tavan 3 — cırcır; yalnız azalır)\n" "P-36c: ORTAK_GIRDI_ISTISNA" "$P36C3_ISTISNA_N"
else
  printf "  ✗ %-46s %s > tavan 3\n" "P-36c: ORTAK_GIRDI_ISTISNA" "$P36C3_ISTISNA_N"; HATA=1
fi

# ── GIRDI_AILESI_KULLANIM (cırcır; YALNIZ ARTAR) ───────────────────────────
# Göçün ÜRÜNÜ: ekranlardaki ortak girdi bileşeni kullanımı. Taban 2026-09-21
# ölçümüdür; azalırsa biri aileden ham girdiye geri dönmüş demektir.
P36C3_KULLANIM_TABAN=250
P36C3_KULLANIM=$(grep -rhoE '<(Metin|Sayi|Para|Gram|Tarih|TarihAralik|Telefon|Eposta|CokSatir|Onay|Secim|Anahtar|Sifre|Arama|SaltOkunur)[ >]' \
  --include='*.razor' "$WEB/Pages" "$WEB/Bilesenler" "$WEB/Layout" 2>/dev/null | grep -c . || true)
if [ "$P36C3_KULLANIM" -ge "$P36C3_KULLANIM_TABAN" ]; then
  printf "  ✓ %-46s %s (taban %s, yalnız artar)\n" "P-36c: GIRDI_AILESI_KULLANIM" "$P36C3_KULLANIM" "$P36C3_KULLANIM_TABAN"
else
  printf "  ✗ %-46s %s < taban %s — ortak girdiden ham girdiye dönüş\n" \
    "P-36c: GIRDI_AILESI_KULLANIM" "$P36C3_KULLANIM" "$P36C3_KULLANIM_TABAN"; HATA=1
fi
# <<< P-36c

# >>> P-36b — GİRDİ BİLEŞEN AİLESİ + GÖRÜNÜM AYARI (2026-09-21)
# Kimlikler: kurucu talimatı 20/a (ortak girdiler) · talimat 16 (renk/yazı tipi)
#            FRONTEND-KIMLIK § 6.3 (Secici uzak kipi) · FRONTEND-SARTNAME § 6.1-6.2
# ⚠ TÜM TABANLAR 2026-09-21'de TAZE ÖLÇÜLDÜ (belgeden kopyalanmadı).
echo
echo "═══ P-36b girdi ailesi / görünüm cırcırı ═══"

# ── GIRDI_AILESI_TAM = 12 ──────────────────────────────────────────────────
# Kurucu talimatı 20/a: "tüm inputlar için bize ait birer tane component olmalı".
# Ailenin 12 üyesi EKSİKSİZ durur; biri silinirse ekranlar ham <input>'a döner.
# ÖLÇÜM: for a in Metin Sayi Para Gram Tarih TarihAralik Telefon Eposta \
#            CokSatir Onay Secim Anahtar; do ls Kuyumcu.Web/Bilesenler/Form/$a.razor; done
P36B_DIZIN=Kuyumcu.Web/Bilesenler/Form
P36B_UYE=0
# ⚠ Bu betiğin yorumlayıcısı BASH'tir (#!/usr/bin/env bash, :1) — zsh değil.
#   zsh'ta gereken ${=VAR} bölmesi burada "bad substitution" verir; düz $VAR
#   bölmesi doğrudur. (Etkileşimli kabuk zsh olduğu için karıştırılıyor.)
# P-36c/3: aileye ÜÇ üye eklendi — Sifre (parola), Arama (süzgeç kutusu),
# SaltOkunur (türetilen değer). Onlarsız göç ham <input> bırakıyordu.
P36B_LISTE="Metin Sayi Para Gram Tarih TarihAralik Telefon Eposta CokSatir Onay Secim Anahtar Sifre Arama SaltOkunur"
for a in $P36B_LISTE; do
  [ -f "$P36B_DIZIN/$a.razor" ] && P36B_UYE=$((P36B_UYE+1))
done
if [ "$P36B_UYE" = 15 ]; then
  printf "  ✓ %-46s 15/15\n" "P-36b: GIRDI_AILESI_TAM"
else
  printf "  ✗ %-46s %s/15 — eksik bileşen var\n" "P-36b: GIRDI_AILESI_TAM" "$P36B_UYE"; HATA=1
fi

# ── GIRDI_AILESI_TEK_KAYNAK = 1 ────────────────────────────────────────────
# Kurucunun "tek seferde değiştiririz" şartı ancak TEK görsel kabuk ile tutar:
# etiket/zorunluluk/ipucu/hata düzenini YALNIZ AlanKabugu.razor çizer ve ailede
# satır içi renk/px YOKTUR (değerler belirtecler.css'ten gelir).
# ÖLÇÜM: ls Kuyumcu.Web/Bilesenler/Form/AlanKabugu.razor | wc -l              # → 1
#        grep -rlE 'style="[^"]*(#|px|rgb)' Kuyumcu.Web/Bilesenler/Form | wc -l # → 0
P36B_KABUK=$([ -f "$P36B_DIZIN/AlanKabugu.razor" ] && echo 1 || echo 0)
P36B_SATIRICI=$(grep -rlE 'style="[^"]*(#|px|rgb)' "$P36B_DIZIN" 2>/dev/null | wc -l | tr -d ' ')
if [ "$P36B_KABUK" = 1 ] && [ "$P36B_SATIRICI" = 0 ]; then
  printf "  ✓ %-46s 1 (satır içi renk/px: 0)\n" "P-36b: GIRDI_AILESI_TEK_KAYNAK"
else
  printf "  ✗ %-46s kabuk=%s satirici=%s\n" \
    "P-36b: GIRDI_AILESI_TEK_KAYNAK" "$P36B_KABUK" "$P36B_SATIRICI"; HATA=1
fi

# ── GIRDI_AILESI_DISI_BICIM = 0 ────────────────────────────────────────────
# Para/gram/tutar biçimlendirmesi YALNIZ Servisler/Bicim.cs'tedir. P-36b öncesi
# dört razor sayfası kendi ToString("N2"/"N3", Kultur) satırını taşıyordu
# (PusulaBelgesi · GiderPusulalari · Uyum · Panel) ve biri değişse ötekiler
# sapıyordu; Panel ayrıca tutarı "varsayılan TL" sayıyordu (UX-S32 ihlali).
# ⚠ KAPSAM DÜRÜSTÇE DAR: N0 ile basılan ADET sayaçları (KarZarar, DenetimIzi)
#   para/gram DEĞİLDİR ve bu sayaca girmez.
# ÖLÇÜM: grep -rhoE 'ToString\("(C2|N2|N3|F2|F3|F4)"' Kuyumcu.Web/Pages \
#          Kuyumcu.Web/Bilesenler Kuyumcu.Web/Layout --include='*.razor' | wc -l
# TABAN (2026-09-21 paket ÖNCESİ, TAZE): 8  →  paket SONRASI: 0
P36B_BICIM=$(grep -rhoE 'ToString\("(C2|N2|N3|F2|F3|F4)"' Kuyumcu.Web/Pages \
  Kuyumcu.Web/Bilesenler Kuyumcu.Web/Layout --include='*.razor' 2>/dev/null | wc -l | tr -d ' ')
if [ "$P36B_BICIM" = 0 ]; then
  printf "  ✓ %-46s 0 (biçim yalnız Bicim.cs'ten)\n" "P-36b: GIRDI_AILESI_DISI_BICIM"
else
  printf "  ✗ %-46s %s (0 olmalı — Servisler/Bicim.cs kullanılmalı)\n" \
    "P-36b: GIRDI_AILESI_DISI_BICIM" "$P36B_BICIM"; HATA=1
fi

# ── TEMA_KONTRAST_IHLALI = 0 ───────────────────────────────────────────────
# Renk şemasının HER yüzü WCAG 2.1 AA geçmek ZORUNDA. Oran HESAPLANIR, tahmin
# edilmez: TasarimBelirtecTestleri Yıldız'ın aydınlık + karanlık bloklarını ölçer.
# ⚠ TEMA TEMİZLİĞİ (kurucu 17.10): "tema sadece Yıldız, karanlık ve açık mod kalacak".
#   Eski 8 şema bloğu belirtecler.css'ten silindi; ölçülen şema blokları artık
#   css/tema-yildiz.css'tedir. (a) eşiği "≥2 şema" yerine "Yıldız'ın ÜÇ yüzü"
#   oldu: aydınlık + sistem karanlık + zorlanmış karanlık (tek şemada üç blok;
#   gevşeme değil, ölçülen nesne değişti — biri silinirse kırmızı).
# Kabuk burada ÖLÇÜMÜN KOŞULLARINI zorlar (kontrast hesabını değil):
#   (a) Yıldız'ın üç bloğu var,
#   (b) test yükleyicisi data-sema'yı TANIYOR (tanımazsa şema sessizce
#       ölçülmeden kalır — sayacın gerçek riski budur),
#   (c) bloklardaki her RENK bildirimi 6 haneli HEX (alfa/gradyan ölçülemez;
#       --golge-* ve --yazi-* renk değildir, sayılmaz).
#   (d) belirtecler.css'te eski şema bloğu KALMADI (0).
P36B_CSS=Kuyumcu.Web/wwwroot/css/tema-yildiz.css
P36B_BLOK='^(:root\[data-sema="yildiz"\] \{|  :root:not\(\[data-tema="aydinlik"\]\)\[data-sema="yildiz"\] \{|:root\[data-tema="karanlik"\]\[data-sema="yildiz"\] \{)'
P36B_SEMA=$(grep -cE "$P36B_BLOK" "$P36B_CSS" 2>/dev/null | tr -d ' ')
P36B_ESKI=$(grep -cE '^ *:root(:not\([^)]*\))?(\[data-tema="karanlik"\])?\[data-sema=' Kuyumcu.Web/wwwroot/css/belirtecler.css 2>/dev/null | tr -d ' ')
P36B_OLCULUYOR=$(grep -c 'data-sema' Kuyumcu.Tests/TasarimBelirtecTestleri.cs 2>/dev/null | tr -d ' ')
P36B_HEXSIZ=$(awk -v B="$P36B_BLOK" '$0 ~ B {i=1;next} i&&/^ *\}/{i=0} i&&/^ *--/&&!/^ *--(golge|yazi)/{ if ($0 !~ /#[0-9a-fA-F]{6};/) print }' \
  "$P36B_CSS" 2>/dev/null | wc -l | tr -d ' ')
if [ "$P36B_SEMA" -ge 3 ] && [ "$P36B_ESKI" = 0 ] && [ "$P36B_OLCULUYOR" -ge 1 ] && [ "$P36B_HEXSIZ" = 0 ]; then
  printf "  ✓ %-46s 0 (Yıldız %s blok, hepsi ölçülüyor · eski şema bloğu 0)\n" "P-36b: TEMA_KONTRAST_IHLALI" "$P36B_SEMA"
else
  printf "  ✗ %-46s yildiz_blok=%s (≥3) eski=%s (0) olculuyor=%s hexsiz=%s\n" \
    "P-36b: TEMA_KONTRAST_IHLALI" "$P36B_SEMA" "$P36B_ESKI" "$P36B_OLCULUYOR" "$P36B_HEXSIZ"; HATA=1
fi

# ── SECICI_UZAK_KIP = 1 · KAYIT_SECICI_KULLANIM tavan 4 (yalnız azalır) ────
# FRONTEND-KIMLIK § 6.3: "<Secici> KayitSecici'yi YUTAR." P-36b uzak kipi
# Secici'ye ekledi (sunucuda arama + 250ms + yarış koruması); KayitSecici'nin
# 4 kullanımı KALDI ve göç P-36c'ye devredildi. Tavan bugün kilitlenir ki
# BEŞİNCİ kullanım açılmasın — iki bileşen yan yana büyümesin.
# ÖLÇÜM: grep -c 'UzakTur is { Length: > 0 }' Kuyumcu.Web/Bilesenler/Secici.razor  # ≥1
#        grep -rl 'KayitSecici' Kuyumcu.Web/Pages --include='*.razor' | wc -l      # ≤4
P36B_UZAK=$(grep -c 'UzakMi' Kuyumcu.Web/Bilesenler/Secici.razor 2>/dev/null | tr -d ' ')
P36B_KS_TAVAN=4
P36B_KS=$(grep -rl 'KayitSecici' Kuyumcu.Web/Pages --include='*.razor' 2>/dev/null | wc -l | tr -d ' ')
if [ "$P36B_UZAK" -ge 1 ] && [ "$P36B_KS" -le "$P36B_KS_TAVAN" ]; then
  printf "  ✓ %-46s 1 (KayitSecici kullanımı %s, tavan %s)\n" \
    "P-36b: SECICI_UZAK_KIP" "$P36B_KS" "$P36B_KS_TAVAN"
else
  printf "  ✗ %-46s uzak=%s kayitsecici=%s > tavan %s\n" \
    "P-36b: SECICI_UZAK_KIP" "$P36B_UZAK" "$P36B_KS" "$P36B_KS_TAVAN"; HATA=1
fi
# <<< P-36b

# >>> P-37 REVİZYON — MENÜ + ARAÇ ŞERİDİ DÜZEN DİSİPLİNİ
# Kurucu 21 Eylül 16:30 / 16:35 reddi. Sayaçlar CIRCIR: yalnız 0 kabul edilir.
# Taban TAZE ölçüldü (paket öncesi): SECICI_SARTLI_ARAMA=1 · MENU_OK_DUGMESI=2
# MENU_KALICI_PARAGRAF=2 · MUKERRER_ARAMA_YUZEYI=2 · MENU_SESSIZ_YUTMA=0(P-37/1'de kapandı)
echo
echo "── P-37 REVİZYON (menü + araç şeridi düzen disiplini) ──"

# ── SECICI_SARTLI_ARAMA = 0 ───────────────────────────────────────────────
# "dropdownun bazılarında var bazılarında yok arama" — eşik KALDIRILDI.
# ÖLÇÜM: Secici.razor'da AramaEsigi geçişi + kullanan ekran sayısı.
P37_ESIK=$(grep -c 'AramaEsigi' Kuyumcu.Web/Bilesenler/Secici.razor 2>/dev/null | tr -d ' ')
P37_ESIK_KUL=$(grep -rl 'AramaEsigi' Kuyumcu.Web/Pages Kuyumcu.Web/Bilesenler --include='*.razor' 2>/dev/null | wc -l | tr -d ' ')
P37_SARTLI=$((P37_ESIK + P37_ESIK_KUL))
if [ "$P37_SARTLI" = 0 ]; then
  printf "  ✓ %-46s 0 (arama kutusu HER açılışta görünür)\n" "P-37: SECICI_SARTLI_ARAMA"
else
  printf "  ✗ %-46s %s (eşik geri gelmiş)\n" "P-37: SECICI_SARTLI_ARAMA" "$P37_SARTLI"; HATA=1
fi

# ── MENU_OK_DUGMESI = 0 ───────────────────────────────────────────────────
# "menüde ok işaretleri nedir saçma sapan, sürükle bırakla olacak dedim".
# TEMA TEMİZLİĞİ (17.10): menü = YildizUstMenu + YildizEkranlar (KenarCubugu kalktı).
P37_MENU="Kuyumcu.Web/Bilesenler/Kabuk/YildizUstMenu.razor Kuyumcu.Web/Bilesenler/Yildiz/YildizEkranlar.razor"
P37_OK=$(cat $P37_MENU 2>/dev/null | grep -cE 'kenar-tasi|kenar-yukari-|kenar-asagi-' | tr -d ' ')
if [ "$P37_OK" = 0 ]; then
  printf "  ✓ %-46s 0 (sıralama sürükle-bırak + Alt+↑/↓)\n" "P-37: MENU_OK_DUGMESI"
else
  printf "  ✗ %-46s %s (görünür ok düğmesi geri gelmiş)\n" "P-37: MENU_OK_DUGMESI" "$P37_OK"; HATA=1
fi

# ── MENU_KALICI_PARAGRAF = 0 ──────────────────────────────────────────────
# Menüde kalıcı açıklama paragrafı OLMAZ; açıklama ipucuna (title) iner.
P37_PARAGRAF=$(cat $P37_MENU 2>/dev/null | grep -cE '<p class="kenar-grup-ipucu"' | tr -d ' ')
if [ "$P37_PARAGRAF" = 0 ]; then
  printf "  ✓ %-46s 0 (açıklama title ipucunda)\n" "P-37: MENU_KALICI_PARAGRAF"
else
  printf "  ✗ %-46s %s\n" "P-37: MENU_KALICI_PARAGRAF" "$P37_PARAGRAF"; HATA=1
fi

# ── MENU_SESSIZ_YUTMA = 0 ─────────────────────────────────────────────────
# Yetki isteği hata verince menü BOŞALTILMAZ (Anayasa md.1).
# ⚠ Yorum satırları AYIKLANIR: "eskiden burada şu vardı" açıklaması ihlal değildir.
P37_YUTMA=$(sed -E 's,^[[:space:]]*///?.*$,,' Kuyumcu.Web/Servisler/YetkiServisi.cs 2>/dev/null \
  | grep -cE 'catch[^{]*\{[^}]*moduller = \[\]' | tr -d ' ')
P37_HATA_ALANI=$(grep -c 'Hata = ex.Message;' Kuyumcu.Web/Servisler/YetkiServisi.cs 2>/dev/null | tr -d ' ')
if [ "$P37_YUTMA" = 0 ] && [ "$P37_HATA_ALANI" -ge 1 ]; then
  printf "  ✓ %-46s 0 (hata görünür, liste korunur)\n" "P-37: MENU_SESSIZ_YUTMA"
else
  printf "  ✗ %-46s yutma=%s hata_alani=%s\n" "P-37: MENU_SESSIZ_YUTMA" "$P37_YUTMA" "$P37_HATA_ALANI"; HATA=1
fi

# ── MUKERRER_ARAMA_YUZEYI = 0 (/urunler) ──────────────────────────────────
# "inputlar seçme yerleri dengesiz": /urunler'de ÜÇ arama/süzme yüzeyi vardı.
P37_URUN_ARAMA=$(grep -c '<Arama ' Kuyumcu.Web/Pages/Urunler.razor 2>/dev/null | tr -d ' ')
P37_BARKOD_KUTU=$(grep -c 'urun-barkod-kutu' Kuyumcu.Web/Pages/Urunler.razor 2>/dev/null | tr -d ' ')
if [ "$P37_URUN_ARAMA" -le 1 ] && [ "$P37_BARKOD_KUTU" = 0 ]; then
  printf "  ✓ %-46s 0 (/urunler tek arama yüzeyi)\n" "P-37: MUKERRER_ARAMA_YUZEYI"
else
  printf "  ✗ %-46s arama=%s barkod_kutu=%s\n" \
    "P-37: MUKERRER_ARAMA_YUZEYI" "$P37_URUN_ARAMA" "$P37_BARKOD_KUTU"; HATA=1
fi

# ── SAYFA_BASI_DISI_DUZEN = 0 ─────────────────────────────────────────────
# Ekran başına hizalama CSS'i YAZILMAZ: .sayfa-basi* seçicileri YALNIZ app.css'te
# tanımlanır, hiçbir Pages/*.razor kendi başlık düzenini elle kurmaz.
P37_ELLE=$(grep -rlE 'class="[^"]*sayfa-basi' Kuyumcu.Web/Pages --include='*.razor' 2>/dev/null | wc -l | tr -d ' ')
P37_HIZA_TEKYER=$(grep -c 'arac-etiket-bosluk' Kuyumcu.Web/wwwroot/css/app.css 2>/dev/null | tr -d ' ')
if [ "$P37_ELLE" = 0 ] && [ "$P37_HIZA_TEKYER" -ge 1 ]; then
  printf "  ✓ %-46s 0 (hiza TEK yerden: app.css araç şeridi bloğu)\n" "P-37: SAYFA_BASI_DISI_DUZEN"
else
  printf "  ✗ %-46s elle=%s tekyer=%s\n" "P-37: SAYFA_BASI_DISI_DUZEN" "$P37_ELLE" "$P37_HIZA_TEKYER"; HATA=1
fi

# ── KESIK_ETIKET = 0 (kaynak kapısı) ──────────────────────────────────────
# Şeritteki etiket TEK satır ve KESİLMEZ: ellipsis/overflow:hidden konamaz.
P37_ELLIPSIS=$(awk '/\.sayfa-basi-filtre \.alan-etiket,/{i=1} i&&/\}/{i=0} i&&/text-overflow: ellipsis/{c++} END{print c+0}' \
  Kuyumcu.Web/wwwroot/css/app.css 2>/dev/null | tr -d ' ')
P37_CLIP=$(grep -c 'text-overflow: clip' Kuyumcu.Web/wwwroot/css/app.css 2>/dev/null | tr -d ' ')
if [ "$P37_ELLIPSIS" = 0 ] && [ "$P37_CLIP" -ge 1 ]; then
  printf "  ✓ %-46s 0 (etiket tek satır, kesilmez)\n" "P-37: KESIK_ETIKET"
else
  printf "  ✗ %-46s ellipsis=%s clip=%s\n" "P-37: KESIK_ETIKET" "$P37_ELLIPSIS" "$P37_CLIP"; HATA=1
fi
# ── FAVORI_KULLANICI_DISI = 0 (P-37 doğrulama bulgusu, 21 Eylül) ──────────
# Kurucu şartı: favori / menü sırası / panel düzeni KULLANICIYA bağlanır,
# kiracıya DEĞİL. Sunucu tarafı bunu "/tercihler/benim" (oturumdaki kullanıcı)
# ucuyla sağlar. ÖLÇÜM: istemcideki tercih yazma/okuma çağrılarının TAMAMI
# "/tercihler/benim" ile başlar; kiracı geneline yazan bir yol kalmamıştır.
P37_TERCIH_TUM=$(grep -c 'api\.[A-Za-z]*Async<TercihCevap>' Kuyumcu.Web/Servisler/MenuTercihServisi.cs 2>/dev/null | tr -d ' ')
P37_TERCIH_BENIM=$(grep 'api\.[A-Za-z]*Async<TercihCevap>' Kuyumcu.Web/Servisler/MenuTercihServisi.cs 2>/dev/null | grep -c '/tercihler/benim' | tr -d ' ')
P37_TERCIH_KIRACI=$((P37_TERCIH_TUM - P37_TERCIH_BENIM))
# Sunucu ucu da kullanıcı kimliğiyle süzer (kiracı TEK BAŞINA anahtar değildir).
P37_TERCIH_SUNUCU=$(grep -c 'KullaniciId' Kuyumcu.Api/Modules/Tercih/TercihModulu.cs 2>/dev/null | tr -d ' ')
if [ "$P37_TERCIH_TUM" -ge 3 ] && [ "$P37_TERCIH_KIRACI" = 0 ] && [ "$P37_TERCIH_SUNUCU" -ge 1 ]; then
  printf "  ✓ %-46s 0 (tercih yolu %s/%s kullanıcı bazlı)\n" "P-37: FAVORI_KULLANICI_DISI" "$P37_TERCIH_BENIM" "$P37_TERCIH_TUM"
else
  printf "  ✗ %-46s kiraci=%s tum=%s sunucu=%s\n" "P-37: FAVORI_KULLANICI_DISI" "$P37_TERCIH_KIRACI" "$P37_TERCIH_TUM" "$P37_TERCIH_SUNUCU"; HATA=1
fi

# ── KAYNAK AĞACI: YENİ *.bak ARTIĞI = 0 (cırcır) ──────────────────────────
# 21 Eylül doğrulaması 3 adet 0 baytlık *.bak artığı buldu (önceki dalgalardan).
# Silme KURUCUYA bırakıldı (TurkReport hook rm/mv bloklar), bu yüzden mevcutlar
# DONMUŞ beyaz listededir; YENİSİ doğarsa denetim kırılır.
BAK_BEYAZ="Kuyumcu.Tests/AramaHttpTestleri.cs.bak
Kuyumcu.Tests/MadenDefteriTestleri.cs.bak
Kuyumcu.Tests/P21CiroRucuTestleri.cs.bak"
BAK_KACAK=""
for f in $(find . -name '*.bak' -not -path '*/bin/*' -not -path '*/obj/*' | sed 's|^\./||' | sort); do
  echo "$BAK_BEYAZ" | grep -qxF "$f" || BAK_KACAK="$BAK_KACAK $f"
done
if [ -z "$BAK_KACAK" ]; then
  printf "  ✓ %-46s 0\n" "Kaynak ağacı: beyaz liste dışı .bak"
else
  printf "  ✗ %-46s%s\n" "Kaynak ağacı: .bak artığı (beklenen 0)" "$BAK_KACAK"; HATA=1
fi

# ═══ P-37b — KARŞILAMA EKRANI SAYAÇLARI (cırcır, fail-closed) ═════════════

# ── JS_SOZDIZIM_HATASI = 0 ────────────────────────────────────────────────
# ⚠ BU SAYAÇ BİR OLAYDAN DOĞDU (md.1): P-37b koşumunda
#   wwwroot/js/siralistesi.js'in BAŞINDA yarım kalmış 3 satırlık bir artık
#   bulundu; dosya `node --check` ile AYRIŞMIYORDU. Sonucu: tarayıcıda
#   window.kuyumcuSira ve window.kuyumcuKisayol HİÇ TANIMLANMIYOR — yani
#   menü sürükle-bırak ve Ctrl/Cmd+K kısayolu CANLIDA ÇALIŞMIYORDU, hâlbuki
#   birim testleri yeşildi. C# testi bir JS sözdizim hatasını GÖREMEZ.
JS_HATA=""
if command -v node >/dev/null 2>&1; then
  for f in Kuyumcu.Web/wwwroot/js/*.js; do
    node --check "$f" >/dev/null 2>&1 || JS_HATA="$JS_HATA $f"
  done
  if [ -z "$JS_HATA" ]; then
    printf "  ✓ %-46s 0\n" "P-37b: JS_SOZDIZIM_HATASI"
  else
    printf "  ✗ %-46s%s\n" "P-37b: JS_SOZDIZIM_HATASI (beklenen 0)" "$JS_HATA"; HATA=1
  fi
else
  printf "  ✗ %-46s node YOK — doğrulanamadı (md.1: atlanmaz)\n" "P-37b: JS_SOZDIZIM_HATASI"; HATA=1
fi

# ── HAVA_BEKLETME_KAPISI = 1 ──────────────────────────────────────────────
# Hava DIŞ KAYNAKTIR. Karşılama ekranı dış bir sunucunun hızına BAĞLANAMAZ.
# PAKET S1-j (24.09): istemci artık dış kaynağa DOĞRUDAN gitmez (CSP connect-src
# 'self' her Panel açılışında ihlal üretiyordu; CSP gevşetilmez). Kapı iki uçta:
#  İSTEMCİ (Kuyumcu.Web/Servisler/HavaServisi.cs): (1) CancellationTokenSource
#    zaman aşımı, (2) her istisnayı yutup null dönen catch, (3) kendi HttpClient'ı
#    YOK ve mutlak dış adres YOK (yalnız ApiIstemci ile /hava).
#  SUNUCU (Kuyumcu.Api/Modules/Hava/HavaModulu.cs): (4) HttpClient.Timeout,
#    (5) varsayılan KAPALI (GetValue("Hava:Etkin", false)).
HAVA_DOSYA=Kuyumcu.Web/Servisler/HavaServisi.cs
HAVA_API=Kuyumcu.Api/Modules/Hava/HavaModulu.cs
HAVA_IPTAL=$(grep -c 'CancellationTokenSource(TimeSpan.FromSeconds(ZamanAsimiSaniye))' "$HAVA_DOSYA" 2>/dev/null | tr -d ' ')
HAVA_SESSIZ=$(grep -c 'catch (Exception)' "$HAVA_DOSYA" 2>/dev/null | tr -d ' ')
HAVA_DIS=$(grep -cE 'new[[:space:]]+HttpClient|https?://' "$HAVA_DOSYA" 2>/dev/null | tr -d ' ')
HAVA_TIMEOUT=$(grep -c 'istemci.Timeout = ZamanAsimi' "$HAVA_API" 2>/dev/null | tr -d ' ')
HAVA_VARSAYILAN=$(grep -c 'GetValue("Hava:Etkin", false)' "$HAVA_API" 2>/dev/null | tr -d ' ')
if [ "$HAVA_TIMEOUT" -ge 1 ] && [ "$HAVA_IPTAL" -ge 1 ] && [ "$HAVA_SESSIZ" -ge 1 ] && [ "$HAVA_VARSAYILAN" -ge 1 ] && [ "$HAVA_DIS" = 0 ]; then
  printf "  ✓ %-46s 1 (sunucu zaman aşımı + istemci iptal + sessiz düşme + varsayılan KAPALI + doğrudan dış istek 0)\n" "P-37b/S1-j: HAVA_BEKLETME_KAPISI"
else
  printf "  ✗ %-46s timeout=%s iptal=%s sessiz=%s kapali=%s dis=%s\n" "P-37b/S1-j: HAVA_BEKLETME_KAPISI" \
    "$HAVA_TIMEOUT" "$HAVA_IPTAL" "$HAVA_SESSIZ" "$HAVA_VARSAYILAN" "$HAVA_DIS"; HATA=1
fi

# ── KARSILAMA_TEK_ISTEK_BEKLEME = 0 ───────────────────────────────────────
# Kutucuklar birbirini BEKLEMEZ: (a) sayfayı tümden kilitleyen <Yukleniyor />
# kapısı YOK, (b) yükleyiciler tek bir listede PARALEL açılır (await ile
# zincirlenmez), (c) her kutucuğun kendi iskeleti var.
PANEL=Kuyumcu.Web/Pages/Panel.razor
KTIB_KILIT=$(grep -c '<Yukleniyor />' "$PANEL" 2>/dev/null | tr -d ' ')
KTIB_PARALEL=$(grep -c 'Yuklemeler.AddRange' "$PANEL" 2>/dev/null | tr -d ' ')
KTIB_ISKELET=$(grep -c 'karsilama-iskelet' "$PANEL" 2>/dev/null | tr -d ' ')
if [ "$KTIB_KILIT" = 0 ] && [ "$KTIB_PARALEL" -ge 1 ] && [ "$KTIB_ISKELET" -ge 4 ]; then
  printf "  ✓ %-46s 0 (paralel yükleme, %s iskelet)\n" "P-37b: KARSILAMA_TEK_ISTEK_BEKLEME" "$KTIB_ISKELET"
else
  printf "  ✗ %-46s kilit=%s paralel=%s iskelet=%s\n" "P-37b: KARSILAMA_TEK_ISTEK_BEKLEME" \
    "$KTIB_KILIT" "$KTIB_PARALEL" "$KTIB_ISKELET"; HATA=1
fi

# ── KARSILAMA_HAVA_KONUM_IZNI = 0 ─────────────────────────────────────────
# Kurucu şartı (b): şehir işletme ayarlarından gelir, kullanıcıdan KONUM İZNİ
# İSTENMEZ. Tarayıcı Geolocation API'si istemcide hiç çağrılmamalıdır.
KONUM=$(grep -rn 'geolocation' Kuyumcu.Web/Pages Kuyumcu.Web/Servisler Kuyumcu.Web/wwwroot/js 2>/dev/null | wc -l | tr -d ' ')
if [ "$KONUM" = 0 ]; then
  printf "  ✓ %-46s 0\n" "P-37b: KARSILAMA_HAVA_KONUM_IZNI"
else
  printf "  ✗ %-46s %s (konum izni istenmiş)\n" "P-37b: KARSILAMA_HAVA_KONUM_IZNI" "$KONUM"; HATA=1
fi

# ── KARSILAMA_TERCIH_KULLANICI_DISI = 0 ───────────────────────────────────
# Kurucu şartı: kutucuk düzeni (sıra + gizleme) KİRACIYA değil KULLANICIYA
# yazılır. İki taraf birden ölçülür:
#   (a) istemci: MenuTercihServisi'nin TÜM tercih çağrıları /tercihler/benim
#       olmalı — kiracı geneli bir uç (ör. /tercihler/kiraci) sıfır kez geçer;
#   (b) sunucu: kullanici_tercihleri üzerindeki HER sorgu tenant_id YANINDA
#       kullanici_id ile de süzmeli — yalnız tenant_id süzen sorgu = sızıntı.
KTKD_DOSYA=Kuyumcu.Web/Servisler/MenuTercihServisi.cs
KTKD_SUNUCU_DOSYA=Kuyumcu.Api/Modules/Tercih/TercihModulu.cs
KTKD_ISTEMCI=$(grep -o '/tercihler/[^"]*' "$KTKD_DOSYA" 2>/dev/null | grep -v 'benim' | wc -l | tr -d ' ')
KTKD_SUNUCU=$(grep -A2 'db.KullaniciTercihleri' "$KTKD_SUNUCU_DOSYA" 2>/dev/null | grep '\.Where(' | grep -v 'KullaniciId' | wc -l | tr -d ' ')
KTKD_PANEL=$(grep -c 'MenuTercihServisi Tercih' Kuyumcu.Web/Pages/Panel.razor 2>/dev/null | tr -d ' ')
if [ "$KTKD_ISTEMCI" = 0 ] && [ "$KTKD_SUNUCU" = 0 ] && [ "$KTKD_PANEL" -ge 1 ]; then
  printf "  ✓ %-46s 0 (düzen kullanıcı bazlı: istemci 0 · sunucu 0 sızıntı)\n" "P-37b: KARSILAMA_TERCIH_KULLANICI_DISI"
else
  printf "  ✗ %-46s istemci=%s sunucu=%s panel=%s\n" "P-37b: KARSILAMA_TERCIH_KULLANICI_DISI" \
    "$KTKD_ISTEMCI" "$KTKD_SUNUCU" "$KTKD_PANEL"; HATA=1
fi

# <<< P-37 REVİZYON

# ═══ P-37c — PANEL ÖRTÜŞMESİ · ŞERİT KAPSAMI · ARTIKLAR (cırcır) ══════════

# ── PANEL_AKIS_DISI = 0 ───────────────────────────────────────────────────
# ⚠ BU SAYAÇ BİR ÖLÇÜMDEN DOĞDU (md.1): P-37 doğrulayıcısı, araç şeridi dar
#   ekranda İKİNCİ SATIRA sardığında açık dropdown panelinin alt satırın
#   üstüne bindiğini ÖLÇTÜ (1280px'te 5056 px²). Kök sebep panelin
#   `position: absolute` ile AKIŞTAN ÇIKMASIDIR. Şerit kapsamında panel
#   AKIŞTA kalır; kural geri alınırsa denetim kırılır.
# ⚠ TOPLU-4 (TOPLU-2 kapanışı, 02.10): ortak.css (T2-16) DAVRANIŞI DEĞİŞTİRDİ — açık
#   panel şeridi sardırıp tetiği 320 px'e genişletiyor, sayfayı +199…+388 px
#   zıplatıyordu. Bugünkü davranış İKİ katmandır ve sayaç İKİSİNİ de sınar:
#   (a) app.css TABAN: `.sayfa-basi-filtre .secici-liste-kap` akışta (relative,
#       kendi sütun genişliği, z 41) — `.sabit` yerleşimde ve şerit dışında geçerli;
#   (b) ortak.css T2-16: SayfaBasi şeridinde `…secici-liste-kap:not(.sabit)` tetiğe
#       bağlı AKIŞ DIŞI katmandır (absolute); alttaki satıra binme kaygısını açıkken
#       tüm sayfayı kapatan `.secici-perde` (z 40 < panel 41) karşılar.
#   Biri geri alınır ya da perde paneli örterse sayaç kırılır (gevşetilmedi).
P37C_BLOK=$(awk '/^\.sayfa-basi-filtre \.secici-liste-kap \{/{i=1} i{print} i&&/^\}/{i=0}' \
  Kuyumcu.Web/wwwroot/css/app.css 2>/dev/null)
P37C_REL=$(printf '%s' "$P37C_BLOK" | grep -c 'position: relative' | tr -d ' ')
P37C_ABS=$(printf '%s' "$P37C_BLOK" | grep -c 'position: absolute' | tr -d ' ')
P37C_GEN=$(printf '%s' "$P37C_BLOK" | grep -c 'max-width: 100%' | tr -d ' ')
P37C_Z41=$(printf '%s' "$P37C_BLOK" | grep -c 'z-index: 41' | tr -d ' ')
P37C_HIZA=$(grep -c 'sayfa-basi-filtre:has(.secici-liste-kap)' Kuyumcu.Web/wwwroot/css/app.css 2>/dev/null | tr -d ' ')
P37C_T216=$(awk '/^\.sayfa-basi \.sayfa-basi-filtre \.secici-kap > \.secici-liste-kap:not\(\.sabit\) \{/{i=1} i{print} i&&/^\}/{i=0}' \
  Kuyumcu.Web/wwwroot/css/ortak.css 2>/dev/null | grep -c 'position: absolute' | tr -d ' ')
P37C_PERDE=$(grep -c '^\.secici-perde {.*z-index: 40' Kuyumcu.Web/wwwroot/css/app.css 2>/dev/null | tr -d ' ')
if [ "$P37C_REL" -ge 1 ] && [ "$P37C_ABS" = 0 ] && [ "$P37C_GEN" -ge 1 ] && [ "$P37C_Z41" -ge 1 ] && [ "$P37C_HIZA" -ge 1 ] \
   && [ "$P37C_T216" -ge 1 ] && [ "$P37C_PERDE" -ge 1 ]; then
  printf "  ✓ %-46s 0 (taban akışta · şeritte T2-16 tetiğe bağlı katman + perde z40<41)\n" "P-37c: PANEL_AKIS_DISI"
else
  printf "  ✗ %-46s rel=%s abs=%s gen=%s z41=%s hiza=%s t216=%s perde=%s\n" "P-37c: PANEL_AKIS_DISI" \
    "$P37C_REL" "$P37C_ABS" "$P37C_GEN" "$P37C_Z41" "$P37C_HIZA" "$P37C_T216" "$P37C_PERDE"; HATA=1
fi

# ── SAYFA_BASI_KAPSAM = 45/45 (46 ekran − 1 YAZILI muafiyet) ──────────────
# Ortak araç şeridini kullanmayan ekran kalmaz. TEK muaf ekran Panel.razor'dur
# ve gerekçesini KENDİ dosyasında yazılı taşır (SAYFA_BASI_MUAFIYETI). Sessiz
# istisna YOKTUR: muafiyet işareti silinirse sayaç kırılır.
P37C_TOPLAM=$(ls Kuyumcu.Web/Pages/*.razor 2>/dev/null | wc -l | tr -d ' ')
P37C_SERIT=$(grep -l '<SayfaBasi' Kuyumcu.Web/Pages/*.razor 2>/dev/null | wc -l | tr -d ' ')
P37C_MUAF=$(grep -l 'SAYFA_BASI_MUAFIYETI' Kuyumcu.Web/Pages/*.razor 2>/dev/null | wc -l | tr -d ' ')
P37C_ACIK=$((P37C_TOPLAM - P37C_SERIT - P37C_MUAF))
# PAKET S1-d: ikinci YAZILI muafiyet Ekranlar.razor — tam ekran karo menü,
#   tuvalin koyu başlık şeridini (#ekranlar-ust) taşır; gerekçe dosyasında.
# PAKET T-K3 (orkestratör kararı 25.09 02:00): üçüncü YAZILI muafiyet Ayarlar.razor —
#   kurucunun tuvali (Ayarlar.dc.html) başlığı ve sekmeleri ÜST BARA koyar; sayfa
#   içi SayfaBasi tuvalle çelişir. Tavan 2 → 3; yeni muafiyet yine dosyada gerekçe ister.
# TEMA TEMİZLİĞİ (kurucu 17.10, tek şema Yıldız): dördüncü YAZILI muafiyet Cariler.razor — SayfaBasi yalnız silinen
#   eski özgün listedeydi; tuval Musteri4 (YildizMusteriler) kendi başlığını taşır. Gerekçe dosyada; aynı istisna
#   P37cPanelVeKapsamTestleri'nde "dosya YildizMusteriler çizmiyorsa kırılır" koşuluyla kilitli. Tavan 3 → 4.
if [ "$P37C_ACIK" = 0 ] && [ "$P37C_MUAF" -le 4 ]; then
  printf "  ✓ %-46s %s/%s ekran (+%s yazılı muafiyet)\n" "P-37c: SAYFA_BASI_KAPSAM" \
    "$P37C_SERIT" "$((P37C_TOPLAM - P37C_MUAF))" "$P37C_MUAF"
else
  printf "  ✗ %-46s toplam=%s serit=%s muaf=%s acik=%s\n" "P-37c: SAYFA_BASI_KAPSAM" \
    "$P37C_TOPLAM" "$P37C_SERIT" "$P37C_MUAF" "$P37C_ACIK"; HATA=1
fi

# ── ARTIK_DOLU_BAYT = 0 ───────────────────────────────────────────────────
# ⚠ KURAL (P-37c, bundan sonrası için BAĞLAYICI): mutasyon yedeği ARTIK
#   PROJE AĞACINA KOPYALANMAZ — scratchpad altına alınır
#   (…/scratchpad/mut-yedek/<paket>/) ve `cmp` ile oradan geri doğrulanır.
#   Ağaçta kalmış eski artıklar SİLİNEMEZ (TurkReport kancası rm/mv bloklar);
#   `printf '' > dosya` ile BOŞALTILIR, silme K-4 olarak kurucudadır.
#   Bu sayaç "dosya var mı"yı değil, DOLU olup olmadığını ölçer.
ARTIK_DOLU=$(find . \( -name '*.mutorj' -o -name '*.bak' -o -name '*.mutbak' -o -name '*.orig' \) \
  -not -path '*/bin/*' -not -path '*/obj/*' -size +0 2>/dev/null | sed 's|^\./||' | sort | tr '\n' ' ')
if [ -z "$ARTIK_DOLU" ]; then
  printf "  ✓ %-46s 0 bayt\n" "P-37c: ARTIK_DOLU_BAYT"
else
  printf "  ✗ %-46s%s\n" "P-37c: ARTIK_DOLU_BAYT (beklenen 0)" " $ARTIK_DOLU"; HATA=1
fi

# <<< P-37c

# ═════════════════════════════════════════════════════════════════════════════
# >>> P-39 — TASARIM DENETİMİ KÖK NEDENLERİ (8 kök · 43 ekran · 2026-09-21)
#
# Kaynak ölçüm: dokumanlar/09-uygulama-kayitlari/TASARIM-DENETIMI-2026-09-21.md
#   (43 rota + 25 açılan form = 68 anahtar × 4 genişlik = 267 ölçüm satırı,
#    tamamı getBoundingClientRect ile; göz kararı YOK).
#
# ⚠ NE ÖLÇÜLÜR (Anayasa md.1): `denetim.sh` TARAYICI DEĞİLDİR, piksel
#   hesaplamaz. Buradaki 8 sayaç, piksel kusurunu DOĞURAN MEKANİZMANIN
#   kaynakta durduğunu ve ekran başına yamayla geri gelmediğini ölçer.
#   Piksel tarafı denetçinin betiğiyle tarayıcıda ölçülür; betik
#   dokumanlar/09-uygulama-kayitlari/betikler/p39-tasarim-olcum.js
#   dosyasındadır ve sayıları rapora YAZILIR (§ Canlı ölçüm).
#
# Sayaçlar (hepsi fail-closed, cırcır — tavan ARTIRILMAZ):
#   UST_BAR_TASMA_1024 = 0 · FORM_SATIR_HIZA_SAPMASI = 0 ·
#   DOKUNMA_HEDEFI_KUCUK = 0 · DUGME_YUKSEKLIK_CESIDI <= 2 ·
#   TABLO_SARMALAYICISIZ = 0 · PANEL_VIEWPORT_DISI = 0 ·
#   ONIZLEME_MUKERRER = 0 · KONTROL_YUKSEKLIK_SAPMASI = 0
# ═════════════════════════════════════════════════════════════════════════════
echo
echo "═══ P-39 — tasarım kök nedenleri ═══"

P39_CSS="$WEB/wwwroot/css/app.css"
P39_TOK="$WEB/wwwroot/css/belirtecler.css"

p39_ok()  { printf "  ✓ %-46s %s\n" "$1" "$2"; }
p39_kir() { printf "  ✗ %-46s %s\n" "$1" "$2"; HATA=1; }

# ── 1) UST_BAR_TASMA_1024 = 0 (K1) ──────────────────────────────────────────
# Ölçüldü: 1024 px'te 43 rotanın TAMAMI 91-160 px yatay kayıyordu; kaynak
# `header.ust-bar > div.ust-sag` içindeki `#ust-mod-anahtar`. Sarma kuralı
# yalnız `@media (max-width:768px)` içindeydi. Sayaç: sarmanın MEDYA SORGUSU
# DIŞINDA (taban kuralda) durduğunu ölçer. Ayrıca taşmanın gizlenmediğini de.
P39_UST=0
grep -qE '^\.ust-bar   \{ flex-wrap: wrap;' "$P39_CSS" || P39_UST=$((P39_UST+1))
grep -qE '^\.ust-sag   \{ flex-wrap: wrap;' "$P39_CSS" || P39_UST=$((P39_UST+1))
# Taşma GİZLENMEZ (md.1): üst barda overflow-x:hidden kaçağı olmayacak.
if grep -nE '^\.(ust-bar|ust-sag|icerik-sutunu)[^{]*\{[^}]*overflow-x: *hidden' "$P39_CSS" >/dev/null; then
  P39_UST=$((P39_UST+1))
fi
if [ "$P39_UST" = 0 ]; then p39_ok "P-39: UST_BAR_TASMA_1024" "0 (taban sarma kuralı yerinde)"
else p39_kir "P-39: UST_BAR_TASMA_1024 (beklenen 0)" "$P39_UST"; fi

# ── 2) FORM_SATIR_HIZA_SAPMASI = 0 (K2) ─────────────────────────────────────
# 28 kesin hiza kusurunun TEK kökü: alan hücresi etikete göre hizalanıyordu.
# Mekanizma: [etiket|kontrol|alt] üç raylı hücre + satır kapsayıcısında
# `subgrid`. Sayaç (a) mekanizmanın varlığını, (b) ekran başına yamanın
# (satır içi `style="align-items…"`) GERİ GELMEDİĞİNİ ölçer.
P39_HIZA=0
grep -q 'grid-template-rows: subgrid' "$P39_CSS" || P39_HIZA=$((P39_HIZA+1))
grep -q '\.alan-kontrol' "$P39_CSS" || P39_HIZA=$((P39_HIZA+1))
grep -q 'class="alan-kontrol"' "$WEB/Bilesenler/Form/AlanKabugu.razor" || P39_HIZA=$((P39_HIZA+1))
grep -q 'class="alan-alt"'     "$WEB/Bilesenler/Form/AlanKabugu.razor" || P39_HIZA=$((P39_HIZA+1))
P39_SATIRICI=$(grep -rn --include='*.razor' 'style="align-items' "$WEB/Pages" "$WEB/Bilesenler" "$WEB/Layout" 2>/dev/null | wc -l | tr -d ' ')
P39_HIZA=$((P39_HIZA + P39_SATIRICI))
if [ "$P39_HIZA" = 0 ]; then p39_ok "P-39: FORM_SATIR_HIZA_SAPMASI" "0 (subgrid + satır içi hizalama yok)"
else p39_kir "P-39: FORM_SATIR_HIZA_SAPMASI (beklenen 0)" "$P39_HIZA (satır içi: $P39_SATIRICI)"; fi

# ── 3) DOKUNMA_HEDEFI_KUCUK = 0 @375 (K3) ───────────────────────────────────
# Ölçüldü: 375 px'te ekran içi 455 tıklanabilir öğenin 443'ü (%97,4) 44 px
# altındaydı; 5 kabuk düğmesi HER ekranda tekrar ediyordu. Mekanizma:
# telefonda yoğunluk/mod tercihi dokunma hedefini 44 px altına İNDİREMEZ.
P39_DOK=0
grep -q 'dokunma-hedefi: 2.75rem' "$P39_TOK" || P39_DOK=$((P39_DOK+1))
awk '/@media \(max-width: 480px\)/{b=1} b&&/--dokunma-hedefi: 2.75rem/{f=1} END{exit !f}' "$P39_TOK" \
  || P39_DOK=$((P39_DOK+1))
awk '/@media \(max-width: 480px\)/{b=1} b&&/min-width: var\(--dokunma-hedefi\)/{f=1} END{exit !f}' "$P39_CSS" \
  || P39_DOK=$((P39_DOK+1))
if [ "$P39_DOK" = 0 ]; then p39_ok "P-39: DOKUNMA_HEDEFI_KUCUK" "0 (≤480px kilidi yerinde)"
else p39_kir "P-39: DOKUNMA_HEDEFI_KUCUK (beklenen 0)" "$P39_DOK"; fi

# ── 4) DUGME_YUKSEKLIK_CESIDI <= 2 (K4) ─────────────────────────────────────
# Ölçüldü: sistem genelinde 15 farklı düğme yüksekliği (22-71 px).
# Sayaç: düğme/sekme seçicilerinde bildirilen AYRI yükseklik değeri sayısı.
# Beklenen iki değer: var(--dugme-yuksekligi) ve var(--dugme-yuksekligi-kucuk).
P39_DY=$(grep -oE '^\.(dugme|dugme-kucuk|dugme-benzeri|sekme|btn|pill)[^{]*\{[^}]*min-height: *[^;]+' "$P39_CSS" \
  | sed -E 's/.*min-height: *//' | sort -u | wc -l | tr -d ' ')
if [ "$P39_DY" -le 2 ]; then p39_ok "P-39: DUGME_YUKSEKLIK_CESIDI (tavan 2)" "$P39_DY"
else p39_kir "P-39: DUGME_YUKSEKLIK_CESIDI (tavan 2)" "$P39_DY — yeni düğme ölçeği açılmış"; fi

# ── 5) TABLO_SARMALAYICISIZ = 0 (K5) ────────────────────────────────────────
# Ölçüldü: `/hurda` @375'te `main.ana-icerik`in KENDİSİ 58 px yatay kayıyordu;
# sebep `tablo-sarmal` sınıfının CSS'te TANIMSIZ olması (ölü sınıf).
# Sayaç: kendi kaydırma kabında OLMAYAN <table> sayısı.
P39_TBL=$(python3 - <<'PYEOF'
import re, pathlib
kok = pathlib.Path("Kuyumcu.Web")
# YAZILI MUAFİYET: `belge-tablo` = A4 çıktı belgesi (A4Belge, PusulaBelgesi).
# Kâğıtta yatay kaydırma kabı YOKTUR; o tablolar sayfa genişliğine sığacak
# şekilde tasarlanmıştır ve ekranda `.yazdirma-*` kuralıyla ölçeklenir.
# ⚠ M39-5 dersi: "tablo-sar" alt dizgesi ölü `tablo-sarmal` sınıfıyla da
# eşleşiyordu — sarmalayıcı sınıfı TAM yazılır.
sarici = ('class="tablo-sar"', "kart-liste", "KartListe", "yazdirma-tablo", "belge-tablo")
acik = 0
for f in list(kok.rglob("*.razor")):
    m = f.read_text(encoding="utf-8")
    for t in re.finditer(r"<table", m):
        # Sarmalayıcı ETİKETTEN ÖNCE, muafiyet sınıfı ETİKETİN KENDİSİNDE aranır.
        kapsam = m[max(0, t.start() - 600):t.start() + 120]
        if not any(s in kapsam for s in sarici):
            acik += 1
print(acik)
PYEOF
)
if [ "$P39_TBL" = 0 ]; then p39_ok "P-39: TABLO_SARMALAYICISIZ" "0"
else p39_kir "P-39: TABLO_SARMALAYICISIZ (beklenen 0)" "$P39_TBL"; fi

# ── 6) PANEL_VIEWPORT_DISI = 0 (K6) ─────────────────────────────────────────
# Ölçüldü: 5 vakada açılır panel görüş alanının TAMAMEN altında açılıyordu
# (54-471 px). Mekanizma: js/secici-yerlesim.js + `.secici-liste-kap.yukari`.
P39_PNL=0
grep -q 'secici-liste-kap.yukari' "$P39_CSS" || P39_PNL=$((P39_PNL+1))
grep -q 'window.kuyumcuSecici' "$WEB/wwwroot/js/secici-yerlesim.js" 2>/dev/null || P39_PNL=$((P39_PNL+1))
grep -q 'js/secici-yerlesim.js' "$WEB/wwwroot/index.html" || P39_PNL=$((P39_PNL+1))
grep -q 'kuyumcuSecici.yerlestir' "$WEB/Bilesenler/Secici.razor" || P39_PNL=$((P39_PNL+1))
# İkinci yerleşim deseni AÇILMAZ: tek çağıran Secici.razor'dur.
P39_PNL_CAGIRAN=$(grep -rl --include='*.razor' 'kuyumcuSecici' "$WEB/Pages" "$WEB/Bilesenler" "$WEB/Layout" 2>/dev/null | wc -l | tr -d ' ')
[ "$P39_PNL_CAGIRAN" = 1 ] || P39_PNL=$((P39_PNL+1))
if [ "$P39_PNL" = 0 ]; then p39_ok "P-39: PANEL_VIEWPORT_DISI" "0 (tek yerleşim mekanizması)"
else p39_kir "P-39: PANEL_VIEWPORT_DISI (beklenen 0)" "$P39_PNL"; fi

# ── 7) ONIZLEME_MUKERRER = 0 (K7) ───────────────────────────────────────────
# Ölçüldü: 8 alan / 5 ekranda aynı değer hem girdinin İÇİNDE hem ALTINDA
# yazıyordu (`/fiyatlandirma`da birebir aynı: "100" / "100").
# Sayaç: `alan-onizleme` çizen ama `OnizlemeGorunur` kapısından geçmeyen
# bileşen sayısı (ikinci bir önizleme deseni açılırsa da yakalanır).
P39_ONZ=$(python3 - <<'PYEOF'
import pathlib, re
kok = pathlib.Path("Kuyumcu.Web")
acik = 0
for f in kok.rglob("*.razor"):
    m = f.read_text(encoding="utf-8")
    if 'class="alan-onizleme"' not in m:
        continue
    for sp in re.finditer(r'class="alan-onizleme"', m):
        onceki = m[max(0, sp.start() - 500):sp.start()]
        if "OnizlemeGorunur" not in onceki:
            acik += 1
print(acik)
PYEOF
)
if [ "$P39_ONZ" = 0 ]; then p39_ok "P-39: ONIZLEME_MUKERRER" "0"
else p39_kir "P-39: ONIZLEME_MUKERRER (beklenen 0)" "$P39_ONZ"; fi

# ── 7b) ONIZLEME_OLCUTU_RAKAM_DIZISI = 0 (kurucu bulgusu 2026-09-22) ────────
# NEDEN VAR: yukarıdaki 7. sayaç YEŞİLKEN kusur CANLIDA duruyordu. O sayaç
# "önizleme OnizlemeGorunur kapısının arkasında mı" diye sorar; kapının
# İÇİNDEKİ ölçütü göremez. Ölçüt rakam dizisi karşılaştırmasıydı
# (`Rakamlar(ham)` ⊂ `Rakamlar(OnizlemeBicimi)`) ve sondaki/baştaki sıfır
# diziyi bozduğu için sızıyordu. 5198'de ÖLÇÜLDÜ (/islemler Adet kutusu):
#   "1,0" → "1" · "15,20" → "15.2" · "2,50" → "2.5" · "007" → "7"
# MEKANİZMA: ölçüt SAYISAL DEĞER üzerinden işler (decimal ölçeği yok sayar).
# Ölçüm komutu (taban 2026-09-22'de TAZE ölçüldü, sonuç 0):
#   grep -nE 'Rakamlar|Regex\.Replace\([^,]*, *"\[\^0-9\]"' \
#        Kuyumcu.Web/Bilesenler/Form/SayisalAlan.cs | wc -l
P39_OLC=$(grep -cE 'Rakamlar|Regex\.Replace\([^,]*, *"\[\^0-9\]"' \
          "$WEB/Bilesenler/Form/SayisalAlan.cs" 2>/dev/null | tr -d ' ')
# Ölçüt gerçekten değer karşılaştırması mı (tek doğru yol):
grep -q 'return Ondalik(ham) != Deger;' "$WEB/Bilesenler/Form/SayisalAlan.cs" \
  || P39_OLC=$((P39_OLC+1))
if [ "$P39_OLC" = 0 ]; then p39_ok "P-39: ONIZLEME_OLCUTU_RAKAM_DIZISI" "0 (ölçüt sayısal değer)"
else p39_kir "P-39: ONIZLEME_OLCUTU_RAKAM_DIZISI (beklenen 0)" "$P39_OLC"; fi

# ── 8) KONTROL_YUKSEKLIK_SAPMASI = 0 (K8) ───────────────────────────────────
# Ölçüldü: metin kutusu 34,5 px ↔ seçici tetiği 32 px ↔ bazıları 37 px
# (31 yükseklik kaydı / 10 grup). Mekanizma: tek belirteç
# `--kontrol-yuksekligi`. Sayaç: bu ailede SABİT (belirteçsiz) yükseklik
# bildirimi sayısı + ortak kuralın varlığı.
P39_KY=0
grep -q -- '--kontrol-yuksekligi' "$P39_TOK" || P39_KY=$((P39_KY+1))
grep -q 'height: var(--kontrol-yuksekligi, 2.25rem)' "$P39_CSS" || P39_KY=$((P39_KY+1))
P39_SABIT=$(grep -oE '^\.(girdi|secim|secici-tetik|secici-ara)[^{]*\{[^}]*(min-)?height: *[0-9]' "$P39_CSS" | wc -l | tr -d ' ')
P39_KY=$((P39_KY + P39_SABIT))
if [ "$P39_KY" = 0 ]; then p39_ok "P-39: KONTROL_YUKSEKLIK_SAPMASI" "0 (tek belirteç)"
else p39_kir "P-39: KONTROL_YUKSEKLIK_SAPMASI (beklenen 0)" "$P39_KY (sabit: $P39_SABIT)"; fi

# <<< P-39


# >>> P-38 — TUR 7'NİN SAHİPSİZ BULGULARI (cırcır; TRIYAJ-TUR7 § 6)
#
# Üç sayaç da 2026-09-22'de TAZE ölçüldü (belgeden kopyalanmadı); tavan
# ARTIRILMAZ, yalnız azalır.
p38_ok()  { printf "  ✓ %-46s %s\n" "$1" "$2"; }
p38_kir() { printf "  ✗ %-46s %s\n" "$1" "$2"; HATA=1; }

# ── 1) PANO_MARJ_SINIRSIZ = 0 ───────────────────────────────────────────────
# B-18 (#112): −999.999 TL marj 200 ile kabul ediliyor, panoda "gram altın
# alış ₺0,00" çıkıyordu. Mekanizma: kaydetme kapısı kalemin CANLI KOTASYONUNU
# görür ve fiyatı sıfıra indiren marjı reddeder; hesap tarafı da 0'a çekmek
# yerine satırı MARJ_GECERSIZ ile atlar.
# Ölçüm (üç şart; her eksik +1):
#   grep -c 'Dogrula(PanoKalemAyari ayar, MKur.Parite? kotasyon' PanoFiyatServisi.cs
#   grep -c 'return null' (Uygula içinde 0'a çekme KALMADI: 'return 0m' yok)
#   grep -c 'Dogrula(yeni, await Kotasyon(' PanoModulu.cs
P38_PANOSRV="Kuyumcu.Api/Modules/Pano/PanoFiyatServisi.cs"
P38_PANOMOD="Kuyumcu.Api/Modules/Pano/PanoModulu.cs"
P38_MARJ=0
grep -q 'Dogrula(PanoKalemAyari ayar, MKur.Parite? kotasyon' "$P38_PANOSRV" || P38_MARJ=$((P38_MARJ+1))
grep -q 'AtlamaSebebi.MarjGecersiz' "$P38_PANOSRV" || P38_MARJ=$((P38_MARJ+1))
grep -q 'if (fiyat <= 0m) return 0m;' "$P38_PANOSRV" && P38_MARJ=$((P38_MARJ+1))
grep -q 'Dogrula(yeni, await Kotasyon(' "$P38_PANOMOD" || P38_MARJ=$((P38_MARJ+1))
if [ "$P38_MARJ" = 0 ]; then p38_ok "P-38: PANO_MARJ_SINIRSIZ" "0"
else p38_kir "P-38: PANO_MARJ_SINIRSIZ (beklenen 0)" "$P38_MARJ"; fi

# ── 2) OLUSTURMADA_YUTULAN_ALAN = 0 ─────────────────────────────────────────
# B-70 (#163) sınıfı: sunucunun güncellemede kabul ettiği bir alan oluşturmada
# YOKSA, kullanıcı o alanı forma yazar ve alan sessizce kaybolur (UX-S26).
# Ölçüm: her modülün *GuncelleDto / *PatchDto alan kümesi, eşleşen
# *OlusturDto / *GirisDto kümesinden çıkarılır. PATCH üç-durum bayrakları
# (…Verildi/…Gonderildi/…Temizle) ve yalnız güncellemede anlamlı olan
# `Aktif` / `IptalGerekce` sayılmaz. TAZE ÖLÇÜM (2026-09-22): 0.
P38_YUTULAN=$(python3 - <<'PYEOF'
import pathlib, re
kok = pathlib.Path("Kuyumcu.Api/Modules")
siniflar = {}
for f in kok.rglob("*.cs"):
    m = f.read_text(encoding="utf-8")
    for sp in re.finditer(r'class\s+(\w*Dto)\b', m):
        ad = sp.group(1); i = m.find("{", sp.end())
        if i < 0: continue
        d = 0; j = i
        while j < len(m):
            if m[j] == "{": d += 1
            elif m[j] == "}":
                d -= 1
                if d == 0: break
            j += 1
        siniflar[ad] = set(re.findall(r'public\s+[\w\?<>,\s\[\]]+?\s(\w+)\s*\{\s*get', m[i:j]))
yoksay = re.compile(r'(Verildi|Gonderildi|Temizle)$')
ozel = {"Aktif", "IptalGerekce"}
toplam = 0
for ad in siniflar:
    if not re.search(r'(Guncelle|Patch)Dto$', ad): continue
    k = re.sub(r'(Guncelle|Patch)Dto$', '', ad)
    for son in ("OlusturDto", "GirisDto", "Dto", "EkleDto", "YeniDto"):
        if k + son in siniflar and k + son != ad:
            toplam += len({a for a in siniflar[ad]
                           if not yoksay.search(a) and a not in ozel} - siniflar[k + son])
            break
print(toplam)
PYEOF
)
if [ "$P38_YUTULAN" = 0 ]; then p38_ok "P-38: OLUSTURMADA_YUTULAN_ALAN" "0"
else p38_kir "P-38: OLUSTURMADA_YUTULAN_ALAN (beklenen 0)" "$P38_YUTULAN"; fi

# ── 3) PASIF_KAYIT_SECILEBILIR = 0 ──────────────────────────────────────────
# B-66 (#67, #107): "Pasife Al" onayı "yeni satışta seçilemez" diyordu ama
# /islemler ürün listesi süzgeçsiz çekiyordu. Kural: Pages altındaki SEÇİM
# listeleri `/cari?` ve `/urunler?` uçlarını `aktif=true` ile çağırır.
# Yönetim ekranları (Cariler, Urunler) süzgeci kullanıcıdan alır ve sorguyu
# string.Join ile kurar — onlar sayılmaz.
# Ölçüm: grep -rn '"/cari?\|"/urunler?\|/cari?\|/urunler?' Pages | aktif= yok | string.Join yok
P38_PASIF=$(grep -rnE '/(cari|urunler)\?' Kuyumcu.Web/Pages/ \
  | grep -v 'aktif=' | grep -v 'string.Join' | wc -l | tr -d ' ')
if [ "$P38_PASIF" = 0 ]; then p38_ok "P-38: PASIF_KAYIT_SECILEBILIR" "0"
else p38_kir "P-38: PASIF_KAYIT_SECILEBILIR (beklenen 0)" "$P38_PASIF"; fi

# <<< P-38


# >>> P-23 — ÖDEME KALEMİ TABLOSU (R-17/R-18) · CIRCIR
#
# ⚠ SAYAÇLAR P-23c'DE (2026-09-22) YENİDEN YAZILDI. Üç sayaç da TAZE ölçülür;
#   tavan ARTIRILMAZ, yalnız azalır. İkisi CANLI VERİTABANINI sorgular: metin
#   taramasıyla "kapı duruyor mu" sormak, adının söylediği şeyi ÖLÇMEK DEĞİLDİR
#   (hasım doğrulayıcı bulgusu · Anayasa md.1).
p23_ok()  { printf "  ✓ %-46s %s\n" "$1" "$2"; }
p23_kir() { printf "  ✗ %-46s %s\n" "$1" "$2"; HATA=1; }
p23_uya() { printf "  ⚠ %-46s %s\n" "$1" "$2"; }

# ── 1) HAREKETLERDE_ODEME_YONTEMI_YENI_YOL = 0 ──────────────────────────────
# KURAL: odeme_kalemleri tablosuna yazmanın TEK yolu vardır.
#
# ⚠ BEYAZ LİSTE P-23c'DE DEĞİŞTİ VE DARALDI: eskiden istisna
#   Modules/Satis/SatisModulu.cs idi; artık Data/OdemeKalemFabrikasi.cs'tir.
#   Satış sepeti de yazımını o fabrikadan geçirir. Paket koşulu "kalem yazmayan
#   yol beyaz listeye GİREMEZ" der; tek kapı olduğunda giremeyecek bir yol
#   kalmaz — kalem, ödeme taşıyan her harekette HareketFabrikasi tarafından
#   kendiliğinden doğar.
# ÖLÇÜM:
#   grep -rn 'OdemeKalemleri\.Add' Kuyumcu.Api --include='*.cs' \
#     | grep -v 'Data/OdemeKalemFabrikasi.cs' | wc -l   → 0
P23_YENIYOL=$(grep -rn 'OdemeKalemleri\.Add' "$API" --include='*.cs' 2>/dev/null \
  | grep -v '/obj/\|/bin/\|/Migrations/' \
  | grep -v 'Data/OdemeKalemFabrikasi.cs' | wc -l | tr -d ' ')
if [ "$P23_YENIYOL" = 0 ]; then
  p23_ok "P-23: HAREKETLERDE_ODEME_YONTEMI_YENI_YOL" "0"
else
  p23_kir "P-23: HAREKETLERDE_ODEME_YONTEMI_YENI_YOL (beklenen 0)" "$P23_YENIYOL — ikinci yazma yolu açılmış"
fi

# ── 1b) FABRİKA KAPISI SÖKÜLMEMİŞ ───────────────────────────────────────────
# Kalem yazımı HareketFabrikasi.Ekle içinden çağrılır; bu satır silinirse
# "ödeme yazan yol kalem de yazar" değişmezi sessizce kaybolurdu.
P23_KAPI=$(grep -c 'OdemeKalemFabrikasi\.Otomatik(db, taslak, izi)' \
  "$API/Data/HareketFabrikasi.cs" 2>/dev/null | tr -d ' ')
# Fabrika yolunu ATLAYAN (KalemYazari.Cagiran) beyan sayısı: satış sepeti + storno.
P23_ATLAYAN=$(grep -rn 'KalemYazari\.Cagiran' "$API" --include='*.cs' 2>/dev/null \
  | grep -v '/obj/\|/bin/\|/Migrations/' \
  | grep -v 'Data/OdemeKalemFabrikasi.cs' \
  | grep -vE ':[0-9]+:[[:space:]]*(//|///|\*)' | wc -l | tr -d ' ')
if [ "$P23_KAPI" = 1 ] && [ "$P23_ATLAYAN" -le 2 ]; then
  p23_ok "P-23: ODEME_KALEMI_FABRIKA_KAPISI" "1 (atlayan beyan: $P23_ATLAYAN ≤ 2)"
else
  p23_kir "P-23: ODEME_KALEMI_FABRIKA_KAPISI (kapi=1, atlayan≤2)" \
    "kapi=$P23_KAPI atlayan=$P23_ATLAYAN — kalem kapısı sökülmüş ya da yeni bir yol fabrikayı atlıyor"
fi

# ── 2) ODEME_KALEMSIZ_YOL = 0  (CANLI ÖLÇÜM) ────────────────────────────────
# KURAL: ödeme yazan hareket kalem de yazar.
# "Ödeme yazan hareket" = nakit/veresiye payı > 0 OLAN **ya da** kapalı listedeki
# bir ödeme aracıyla etiketlenmiş hareket. Storno satırı ve stornolanmış satır
# kapsam DIŞIDIR (ters kayıt kalem doğurmaz; kaynağın kalemleri pasifleşir).
P23_CANLI_OLCULDU=0
P23_KALEMSIZ=""
P23_FARK=""
if command -v docker >/dev/null 2>&1 && docker ps --format '{{.Names}}' 2>/dev/null | grep -qx "kuyumcu-mysql"; then
  P23_PAROLA=$(awk -F': ' '/^[[:space:]]*MYSQL_ROOT_PASSWORD:/ {print $2; exit}' ../docker-compose.yml 2>/dev/null)
  if [ -n "${P23_PAROLA:-}" ]; then
    P23_KALEMSIZ=$(docker exec kuyumcu-mysql mysql -uroot -p"$P23_PAROLA" -N kuyumcu_net -e "
      SELECT COUNT(*) FROM hareketler h
       WHERE ((COALESCE(h.nakit_tutar,0)+COALESCE(h.veresiye_tutar,0)) > 0
              OR h.odeme_turu IN ('NAKIT','KART','HAVALE','VERESIYE','DOVIZ'))
         AND h.storno_kaynak_id IS NULL
         AND NOT EXISTS (SELECT 1 FROM hareketler s WHERE s.storno_kaynak_id = h.id)
         AND NOT EXISTS (SELECT 1 FROM odeme_kalemleri k
                          WHERE k.hareket_id = h.id AND k.aktif = 1);" 2>/dev/null | tr -d ' ')
    # ── 3) FIS_TOPLAM_ODEME_KALEMI_FARKI = 0  (GERÇEK FARK) ────────────────
    # Fiş = aynı fis_id'yi taşıyan hareket kümesi; fis_id yoksa hareketin
    # KENDİSİ tek satırlık fiştir (odeme_kalemleri.fis_id de bu değeri taşır).
    # Sayaç, aktif kalem toplamı ile fiş toplamının 1 kuruştan fazla ayrıştığı
    # FİŞ SAYISINI verir — bir kapının varlığını DEĞİL.
    P23_FARK=$(docker exec kuyumcu-mysql mysql -uroot -p"$P23_PAROLA" -N kuyumcu_net -e "
      SELECT COUNT(*) FROM (
        SELECT COALESCE(h.fis_id, h.id) AS fk, ROUND(SUM(ABS(h.tl_tutari)),2) AS ft
          FROM hareketler h
         WHERE ((COALESCE(h.nakit_tutar,0)+COALESCE(h.veresiye_tutar,0)) > 0
                OR h.odeme_turu IN ('NAKIT','KART','HAVALE','VERESIYE','DOVIZ'))
           AND h.storno_kaynak_id IS NULL
           AND NOT EXISTS (SELECT 1 FROM hareketler s WHERE s.storno_kaynak_id = h.id)
         GROUP BY 1) a
      LEFT JOIN (SELECT fis_id, ROUND(SUM(tutar_tl),2) kt
                   FROM odeme_kalemleri WHERE aktif = 1 GROUP BY fis_id) b
        ON b.fis_id = a.fk
      WHERE ABS(a.ft - COALESCE(b.kt,0)) > 0.005;" 2>/dev/null | tr -d ' ')
    [ -n "$P23_KALEMSIZ" ] && [ -n "$P23_FARK" ] && P23_CANLI_OLCULDU=1
  fi
fi

if [ "$P23_CANLI_OLCULDU" -eq 1 ]; then
  if [ "$P23_KALEMSIZ" = 0 ]; then
    p23_ok "P-23: ODEME_KALEMSIZ_YOL (canlı)" "0"
  else
    p23_kir "P-23: ODEME_KALEMSIZ_YOL (beklenen 0)" \
      "$P23_KALEMSIZ hareket ödeme yazdı ama kalem yazmadı"
  fi
  if [ "$P23_FARK" = 0 ]; then
    p23_ok "P-23: FIS_TOPLAM_ODEME_KALEMI_FARKI (canlı)" "0 (gerçek toplam farkı ölçüldü)"
  else
    p23_kir "P-23: FIS_TOPLAM_ODEME_KALEMI_FARKI (beklenen 0)" \
      "$P23_FARK fişte kalem toplamı ≠ fiş toplamı"
  fi
else
  # Sessiz yeşil YOK: ölçülemeyen sayaç "geçti" sayılmaz (Anayasa md.1).
  p23_uya "P-23: ODEME_KALEMSIZ_YOL (canlı)" "ÖLÇÜLEMEDİ (kuyumcu-mysql yok) — sessiz yeşil DEĞİL"
  p23_uya "P-23: FIS_TOPLAM_ODEME_KALEMI_FARKI (canlı)" "ÖLÇÜLEMEDİ (kuyumcu-mysql yok) — sessiz yeşil DEĞİL"
fi

# ── 3b) ÇALIŞMA ZAMANI KAPISI + TESTİ DURUYOR MU ────────────────────────────
# Canlı sayaç GEÇMİŞİ ölçer; kapı GELECEĞİ kapatır. İkisi birlikte aranır.
P23_RTKAPI=$(grep -c 'kalemToplam != Yuvarla2(fisToplam)' "$API/Modules/Satis/SatisModulu.cs" 2>/dev/null | tr -d ' ')
P23_EKLE=$(grep -c 'OdemeKalemFabrikasi\.Yaz(db, yazilanKalemler)' "$API/Modules/Satis/SatisModulu.cs" 2>/dev/null | tr -d ' ')
P23_TEST=$(grep -rl 'Kalem_toplami_fis_toplamina_ESITTIR' "Kuyumcu.Tests" --include='*.cs' 2>/dev/null | wc -l | tr -d ' ')
if [ "$P23_RTKAPI" = 1 ] && [ "$P23_EKLE" = 1 ] && [ "$P23_TEST" -ge 1 ]; then
  p23_ok "P-23: FIS_TOPLAM_CALISMA_ZAMANI_KAPISI" "1 (kapı + test yerinde)"
else
  p23_kir "P-23: FIS_TOPLAM_CALISMA_ZAMANI_KAPISI (beklenen 1)" \
    "kapi=$P23_RTKAPI ekle=$P23_EKLE test=$P23_TEST — toplam denkliği kapısı sökülmüş"
fi

# <<< P-23


# >>> P-24 — MUTABAKAT SÖZLEŞMESİ + KANIT REJİMİ (B-7 · B-28 · B-62) · CIRCIR
#
# KÖK NEDEN: "özet" ile "liste" ayrı sorgudan türüyordu. Aynı ekranda aynı
# sorunun iki cevabı oluyordu (vezne ₺0,00 ↔ detay 2.000; evrak "portföy boş" ↔
# 2 evrak; kâr-zarar 11,45 g ↔ 50,38 has; /denetim-izi kartı ↔ liste toplamı).
# Bu blok kusuru değil, KUSURUN GERİ GELME YOLUNU kapatır.
p24_ok()  { printf "  ✓ %-46s %s\n" "$1" "$2"; }
p24_kir() { printf "  ✗ %-46s %s\n" "$1" "$2"; HATA=1; }

# ── 1) OZET_AYRI_SORGU = 0 ──────────────────────────────────────────────────
# KURAL: süzgeç parametresi ALAN her özet ucu, listeyle AYNI süzgeç nesnesinden
#   geçer. Süzgeç nesnesi `MUTABAKAT-SUZGEC` yorumuyla işaretlidir ve adı
#   `...Suzgeci` ile biter. İkinci bir `.Where(` zinciri açan özet ucu = ihlal.
# ÖLÇÜM (2026-09-22'de TAZE ölçüldü, belgeden kopyalanmadı — sonuç 0):
#   perl ile Modules altındaki her "Task<object> Ozet(" imzası okunur;
#   imzada AktifKullanici dışında parametre varsa gövdesinde "Suzgeci(" aranır.
P24_AYRI=$(perl -0777 -ne '
  my $n = 0;
  while (/public\s+async\s+Task<object>\s+Ozet\s*\((.*?)\)\s*\{(.{0,3000})/gs) {
    my ($sig, $govde) = ($1, $2);
    next unless $sig =~ /,/;                 # yalnız AktifKullanici → süzgeçsiz
    $n++ unless $govde =~ /Suzgeci\s*\(/;
  }
  print $n;
' $(find "$API/Modules" -name '*.cs' -not -path '*/obj/*' -not -path '*/bin/*') 2>/dev/null \
  | awk '{s+=$1} END {print s+0}')
if [ "${P24_AYRI:-9}" = 0 ]; then
  p24_ok "P-24: OZET_AYRI_SORGU" "0"
else
  p24_kir "P-24: OZET_AYRI_SORGU (beklenen 0)" \
    "$P24_AYRI özet ucu listenin süzgecini İKİNCİ KEZ yazıyor — kart ile liste ayrışır"
fi

# ── 1b) MUTABAKAT SÜZGECİ SÖKÜLMEMİŞ ────────────────────────────────────────
# İşaretli süzgeç silinirse sayaç 1'i de sessizce boşa düşerdi (nüfus 0 olur).
P24_SUZGEC=$(grep -rl 'MUTABAKAT-SUZGEC' "$API/Modules" --include='*.cs' 2>/dev/null | wc -l | tr -d ' ')
P24_DENK=$(grep -c 'Mutabakat\.Denk(' "$API/Modules/Denetim/DenetimModulu.cs" 2>/dev/null | tr -d ' ')
if [ "$P24_SUZGEC" -ge 1 ] && [ "${P24_DENK:-0}" -ge 3 ]; then
  p24_ok "P-24: MUTABAKAT_SUZGEC_KAPISI" "süzgeç $P24_SUZGEC dosya · çalışma zamanı denkliği $P24_DENK"
else
  p24_kir "P-24: MUTABAKAT_SUZGEC_KAPISI (süzgeç≥1, denk≥3)" \
    "suzgec=$P24_SUZGEC denk=$P24_DENK — mutabakat kapısı sökülmüş"
fi

# ── 2) MUTABAKAT_TESTI_OLMAYAN_OZET = 0 ─────────────────────────────────────
# KURAL: özet ucu olan HER modülün `Mutabakat_<Modul>_` önekli en az bir testi
#   olur. Test adı sözleşmedir (Kuyumcu.Tests/MutabakatTestleri.cs başlığı).
# ÖLÇÜM (TAZE, 2026-09-22 — nüfus 4: Denetim · Masak · Risk · Yedek, eksik 0):
#   grep -rl 'Task<object> Ozet(AktifKullanici' Kuyumcu.Api/Modules --include='*.cs'
#     → dosya adından "Modulu.cs"/"Servisi.cs" atılır → Kuyumcu.Tests'te aranır
# ⚠ İmza ÇOK SATIRLI olabilir (Denetim/Ozet böyledir); satır bazlı grep nüfusu
#   3 sayıyordu ve sayaç sessizce kolaylaşıyordu. Nüfus perl ile ÇOK SATIRLI okunur.
P24_NUFUS=0; P24_EKSIK=0; P24_EKSIKLER=""
for f in $(perl -0777 -ne 'print "$ARGV\n" if /public\s+async\s+Task<object>\s+Ozet\s*\(\s*AktifKullanici/s' \
           $(find "$API/Modules" -name '*.cs' -not -path '*/obj/*' -not -path '*/bin/*') 2>/dev/null); do
  P24_NUFUS=$((P24_NUFUS + 1))
  ad=$(basename "$f" .cs)
  ad=${ad%Modulu}; ad=${ad%Servisi}
  if ! grep -rq "Mutabakat_${ad}_" "Kuyumcu.Tests" --include='*.cs' 2>/dev/null; then
    P24_EKSIK=$((P24_EKSIK + 1)); P24_EKSIKLER="$P24_EKSIKLER $ad"
  fi
done
if [ "$P24_NUFUS" -ge 4 ] && [ "$P24_EKSIK" = 0 ]; then
  p24_ok "P-24: MUTABAKAT_TESTI_OLMAYAN_OZET" "0 (özet ucu olan $P24_NUFUS modülün hepsi kapsanmış)"
else
  p24_kir "P-24: MUTABAKAT_TESTI_OLMAYAN_OZET (beklenen 0, nüfus ≥4)" \
    "nüfus=$P24_NUFUS eksik=$P24_EKSIK →$P24_EKSIKLER"
fi

# ── 3) B-62 · BİTİŞ GÜNÜ OFF-BY-ONE KAPISI ──────────────────────────────────
# Ham `Tarih < bitis` deseni geri gelirse bitiş günü yine kapsam dışı kalır.
P24_HAM=$(grep -rn 'd\.Tarih < bitis' "$API/Modules" --include='*.cs' 2>/dev/null \
  | grep -v '/obj/\|/bin/' | wc -l | tr -d ' ')
P24_KAPSAYICI=$(grep -c 'UstSinirKapsayici' "$API/Modules/Rapor/Mutabakat.cs" 2>/dev/null | tr -d ' ')
if [ "$P24_HAM" = 0 ] && [ "${P24_KAPSAYICI:-0}" -ge 1 ]; then
  p24_ok "P-24: BITIS_GUNU_OFF_BY_ONE" "0 (kapsayıcı üst sınır tek yerde)"
else
  p24_kir "P-24: BITIS_GUNU_OFF_BY_ONE (beklenen 0)" \
    "ham=$P24_HAM kapsayici=$P24_KAPSAYICI — bitiş günü yine kapsam dışı kalır"
fi

# ── 4) B-28 · GRAFİK KANITI getBBox İLE ÖLÇÜLÜR ─────────────────────────────
# bUnit path DİZGESİNİ ölçer; "tek kategori %100 çizilmiyor" bulgusu ancak
# gerçek tarayıcıda getBBox genişliğiyle görülür. QA turu betiği bunu ister.
P24_QA="../dokumanlar/09-uygulama-kayitlari/betikler/qa-turu.js"
P24_BBOX=$(grep -c 'getBBox' "$P24_QA" 2>/dev/null | tr -d ' ')
if [ "${P24_BBOX:-0}" -ge 2 ]; then
  p24_ok "P-24: GRAFIK_KANITI_GETBBOX" "$P24_BBOX (qa-turu.js şema + istem)"
else
  p24_kir "P-24: GRAFIK_KANITI_GETBBOX (beklenen ≥2)" \
    "$P24_BBOX — grafik kanıtı yine path dizgesine düşmüş (B-28)"
fi

# <<< P-24

# >>> P-26 — TEDARİK ZİNCİRİ + MUHASEBE KÖPRÜSÜ + ÖZEL MATRAH · CIRCIR
#
# KÖK NEDEN: yeni bir modül "para/stok alan ama deftere yazmayan" bir ikinci
# gerçek doğurabilir (sipariş tablosunda kapora alanı, satınalmada kendi stok
# aritmetiği). Ve doğrulanmamış bir vergi kodu ("805") ekrana beyansız basılabilir.
# Bu blok kusuru değil, KUSURUN GERİ GELME YOLUNU kapatır.
p26_ok()  { printf "  ✓ %-46s %s\n" "$1" "$2"; }
p26_kir() { printf "  ✗ %-46s %s\n" "$1" "$2"; HATA=1; }

# ── 1) DEFTERE_YAZMAYAN_PARA_MODULU = 0 ─────────────────────────────────────
# KURAL: para ya da stok hareketi doğuran her modül YA `HareketFabrikasi.Ekle`
#   çağırır YA DA yazan servise devreder ve bunu `DEFTER-DELEGE` işaretiyle
#   beyan eder. İşaretsiz devir = görünmez ikinci para yolu.
# ÖLÇÜM (TAZE, 2026-09-22 — belgeden kopyalanmadı; sonuç 0):
#   her modül dizininde yorumlar SÖKÜLEREK `HareketFabrikasi.Ekle(` sayılır
#   (yorumdaki anma sayaçtan kaçırırdı — P-24 dersi), yoksa `DEFTER-DELEGE` aranır.
# NÜFUS: P-26'nın açtığı/dokunduğu para yolları — Satinalma · Irsaliye · Siparis.
# BEYAZ LİSTE (gerekçeli, YALNIZ KISALIR): Kasa · Banka · Gider · Pos · Hurda ·
#   Vezne · Sayim. Bu yedisi P-26'DAN ÖNCE vardı ve defter hikâyeleri BU PAKETTE
#   DENETLENMEDİ — "doğrudur" denmiyor, "bu paketin kapsamı dışı" deniyor
#   (Anayasa md.1). Her biri kendi paketinde ölçülecek; liste büyüyemez.
P26_NUFUS="Satinalma Irsaliye Siparis"
P26_PARA_EKSIK=0; P26_PARA_DETAY=""
for m in ${P26_NUFUS}; do
  hf=$(find "$API/Modules/$m" -name '*.cs' ! -name '*.mutorj' 2>/dev/null \
    | xargs perl -0777 -ne 's{^\s*//[^\n]*}{}gm; $n++ while /HareketFabrikasi\.Ekle\s*\(/g; END{print $n+0}' 2>/dev/null)
  dg=$(find "$API/Modules/$m" -name '*.cs' ! -name '*.mutorj' 2>/dev/null \
    | xargs grep -l 'DEFTER-DELEGE' 2>/dev/null | wc -l | tr -d ' ')
  if [ "${hf:-0}" -eq 0 ] && [ "${dg:-0}" -eq 0 ]; then
    P26_PARA_EKSIK=$(( P26_PARA_EKSIK + 1 )); P26_PARA_DETAY="$P26_PARA_DETAY $m"
  fi
done
# Irsaliye bilerek defter görmez ve devretmez (sevk belgesi, değer hareketi yok):
# bu yüzden nüfusta DURUR ama kendi beyanını dosya başlığında taşır. Beyanı da
# ölçeriz — beyan silinirse modül sessizce "para modülü" olabilir.
P26_IRS_BEYAN=$(grep -c 'İRSALİYE DEFTERE YAZMAZ' "$API/Modules/Irsaliye/IrsaliyeModulu.cs" 2>/dev/null | tr -d ' ')
if [ "$P26_PARA_EKSIK" -le 1 ] && [ "${P26_IRS_BEYAN:-0}" -ge 1 ]; then
  p26_ok "P-26: DEFTERE_YAZMAYAN_PARA_MODULU" "0 (nüfus 3 · irsaliye beyanlı)"
else
  p26_kir "P-26: DEFTERE_YAZMAYAN_PARA_MODULU (beklenen 0)" \
    "işaretsiz modül:$P26_PARA_DETAY · irsaliye beyanı=$P26_IRS_BEYAN"
fi

# ── 1b) SATINALMA KENDİ STOK/DEFTER ARİTMETİĞİ YAZMASIN ─────────────────────
# Delege işareti dursun ama modül yine de kendi `Stoklar.Add` / `Hareketler.Add`
# yazarsa aynı malın iki maliyeti olur. İkisi de 0 olmalı (yorumlar sökülerek).
P26_IKINCI=$(find "$API/Modules/Satinalma" "$API/Modules/Irsaliye" -name '*.cs' 2>/dev/null \
  | xargs perl -0777 -ne 's{^\s*//[^\n]*}{}gm;
      $n++ while /db\.Stoklar\.Add\s*\(|db\.Hareketler\.Add\s*\(|db\.KasaHareketleri\.Add\s*\(/g;
      END{print $n+0}' 2>/dev/null)
if [ "${P26_IKINCI:-9}" = 0 ]; then
  p26_ok "P-26: TEDARIKTE_IKINCI_YAZMA_YOLU" "0"
else
  p26_kir "P-26: TEDARIKTE_IKINCI_YAZMA_YOLU (beklenen 0)" \
    "$P26_IKINCI — tedarik modülü kendi stok/defter aritmetiğini yazıyor"
fi

# ── 2) OZEL_MATRAH_BEYANSIZ_EKRAN = 0 ───────────────────────────────────────
# KURAL: özel matrah neden kodunu (805) GÖSTEREN her ekran, kodun KAYNAĞINI da
#   aynı ekranda yazar. Kod tek başına "doğrulanmış mevzuat" izlenimi verir;
#   oysa bu bir ekip kararıdır (K-10) ve mali müşavir imzası YOKTUR.
# ÖLÇÜM (TAZE, 2026-09-22 — nüfus 1: Faturalar.razor, beyansız 0):
#   grep -rlE 'ozelMatrah|OzelMatrah|ozel-matrah' Kuyumcu.Web/{Pages,Bilesenler} --include='*.razor'
#   → her dosyada 'ozelMatrahKaynak' ya da 'kaynakBeyani' aranır.
P26_EKRANLAR=$(grep -rlE 'ozelMatrah|OzelMatrah|ozel-matrah' \
  "$WEB/Pages" "$WEB/Bilesenler" --include='*.razor' 2>/dev/null)
P26_EKRAN_NUFUS=$(printf '%s\n' "$P26_EKRANLAR" | grep -c . | tr -d ' ')
P26_BEYANSIZ=0; P26_BEYANSIZ_AD=""
for f in ${P26_EKRANLAR}; do
  if ! grep -qE 'ozelMatrahKaynak|kaynakBeyani' "$f" 2>/dev/null; then
    P26_BEYANSIZ=$(( P26_BEYANSIZ + 1 )); P26_BEYANSIZ_AD="$P26_BEYANSIZ_AD $(basename "$f")"
  fi
done
if [ "$P26_BEYANSIZ" = 0 ] && [ "${P26_EKRAN_NUFUS:-0}" -ge 1 ]; then
  p26_ok "P-26: OZEL_MATRAH_BEYANSIZ_EKRAN" "0 (nüfus $P26_EKRAN_NUFUS)"
else
  p26_kir "P-26: OZEL_MATRAH_BEYANSIZ_EKRAN (beklenen 0)" \
    "beyansız:$P26_BEYANSIZ_AD · nüfus=$P26_EKRAN_NUFUS (nüfus 0 ise sayaç boşa düşmüş)"
fi

# ── 2b) KOD KODA GÖMÜLMESİN (K-10 madde 3: tek alandan döner) ───────────────
# "805" yapılandırmadan gelir; kaynak dosyasına sabit yazılırsa yorum
# değiştiğinde ürün kilitlenir. İzinli tek yer: KdvAyarlari.VarsayilanKod.
# ⚠ Yorumlar SÖKÜLEREK sayılır: "805" bir açıklama satırında geçtiği için
#   sayaç kırılırsa gerçek ihlal görünmez olurdu (P-24 dersi).
P26_GOMULU=$(find "$API" -name '*.cs' ! -name '*.mutorj' 2>/dev/null \
  | grep -v '/obj/\|/bin/\|/Migrations/' | grep -v 'KdvAyarlari.cs' \
  | xargs perl -0777 -ne 's{^\s*//[^\n]*}{}gm; s{///[^\n]*}{}g; $n++ while /"805"/g; END{print $n+0}' 2>/dev/null | tr -d ' ')
if [ "${P26_GOMULU:-9}" = 0 ]; then
  p26_ok "P-26: OZEL_MATRAH_KODU_GOMULU" "0 (yalnız KdvAyarlari.OneriKod)"
else
  p26_kir "P-26: OZEL_MATRAH_KODU_GOMULU (beklenen 0)" \
    "$P26_GOMULU — kod ikinci bir yere sabit yazılmış, yorum değişince dönmez"
fi

# ── 2c) OZEL_MATRAH_TEYITSIZ_ACIK = 0 (hasım bulgusu · P-26 düzeltmesi) ─────
# KURAL (PLAN-TEK-SEFER-2026-09-18.md:578 + :778): K-10 KURUCU/mali müşavir
#   kalemidir; teyit GELMEDEN Y-40 anahtarı AÇILMAZ. Anahtar iki yerden açılır
#   ve ikisi de KAPALI sevk edilmek ZORUNDA: (a) KdvAyarlari.OzelMatrahKodu'na
#   derleme zamanı varsayılan atanamaz, (b) appsettings.json'daki Kdv bölümü
#   boş sevk edilir. Teyit gelince BU SAYAÇ BİLEREK KIRILIR ve açılış ayrı bir
#   kapı kaydına yazılır — sessiz açılış olamaz.
# ÖLÇÜM (TAZE, 2026-09-22 — ikisi de 0):
#   perl -0777 -ne '...OzelMatrahKodu { get; init; } = ...' Modules/Fatura/KdvAyarlari.cs
#   perl -0777 -ne '..."OzelMatrahKodu"\s*:\s*"[^"]+"...' appsettings.json
P26_VARSAYILAN_ATAMA=$(perl -0777 -ne 's{^\s*//[^\n]*}{}gm; s{///[^\n]*}{}g;
  $n++ while /OzelMatrahKodu\s*\{\s*get;\s*init;\s*\}\s*=/g; END{print $n+0}' \
  "$API/Modules/Fatura/KdvAyarlari.cs" 2>/dev/null | tr -d ' ')
P26_AYAR_DOLU=$(perl -0777 -ne '$n++ while /"OzelMatrahKodu"\s*:\s*"[^"]+"/g; END{print $n+0}' \
  "$API/appsettings.json" 2>/dev/null | tr -d ' ')
P26_TEYITSIZ=$(( ${P26_VARSAYILAN_ATAMA:-9} + ${P26_AYAR_DOLU:-9} ))
if [ "$P26_TEYITSIZ" = 0 ]; then
  p26_ok "P-26: OZEL_MATRAH_TEYITSIZ_ACIK" "0 (K-10 beklenir · anahtar kapalı doğuyor)"
else
  p26_kir "P-26: OZEL_MATRAH_TEYITSIZ_ACIK (beklenen 0)" \
    "kod varsayılanı=$P26_VARSAYILAN_ATAMA · appsettings dolu=$P26_AYAR_DOLU — K-10 teyidi olmadan Y-40 açılmış (PLAN:578/778)"
fi

# ── 3) Y-36 · USTAYA METAL ÇIKIŞI K-13 GELMEDEN YAZILMAZ ────────────────────
# Kolon (usta_cari_id) VAR ama defter bacağı YOK. Sipariş modülünde metal
# çıkışı yazan bir hareket türü belirirse bu kapı kırılır.
P26_METAL=$(find "$API/Modules/Siparis" -name '*.cs' ! -name '*.mutorj' 2>/dev/null \
  | xargs perl -0777 -ne 's{^\s*//[^\n]*}{}gm;
      $n++ while /HareketFabrikasi\.Ekle\s*\(|EMANET_CIKIS"\s*,/g; END{print $n+0}' 2>/dev/null)
P26_METAL_BEYAN=$(grep -c 'ustaMetalCikisiBeyani' "$API/Modules/Siparis/SiparisModulu.cs" 2>/dev/null | tr -d ' ')
if [ "${P26_METAL:-9}" = 0 ] && [ "${P26_METAL_BEYAN:-0}" -ge 1 ]; then
  p26_ok "P-26: USTA_METAL_CIKISI_YAZAN_YOL" "0 (K-13 · ekranda beyanlı)"
else
  p26_kir "P-26: USTA_METAL_CIKISI_YAZAN_YOL (beklenen 0)" \
    "yazan yol=$P26_METAL beyan=$P26_METAL_BEYAN — K-13 kapsam sınırı delinmiş"
fi

# <<< P-26

# >>> P-27 — ABONELİK + PLAN KAPISI (F-1 / F-3 · R-1) · CIRCIR
#
# KÖK NEDEN: plan alanı bir TERCİH gibi duruyordu (PATCH /admin/plan, SAHİP
# serbest) oysa ücret doğuran bir HAKTIR. İki kusurun geri gelme yolu kapatılır:
#   (a) plan yazan İKİNCİ bir yol açılması (kapısız uç),
#   (b) plan değişiminin izsiz kalması.
p27_ok()  { printf "  ✓ %-46s %s\n" "$1" "$2"; }
p27_kir() { printf "  ✗ %-46s %s\n" "$1" "$2"; HATA=1; }

# ── 1) YETKISIZ_PLAN_YAZAN_UC = 0 ───────────────────────────────────────────
# KURAL: `tenants.plan` alanına atama YALNIZ AbonelikServisi.cs içinde yapılır.
#   Başka bir serviste `X.Plan =` görülürse, o yol AbonelikServisi'nin seviye
#   kapısını (yükseltme yasağı) ve denetim izini ATLAR.
# ÖLÇÜM (TAZE, 2026-09-22 — sonuç 0; yorumlar SÖKÜLEREK sayılır, P-24 dersi):
#   find Kuyumcu.Api -name '*.cs' (obj/bin/Migrations hariç, AbonelikServisi.cs hariç)
#     | perl -0777 -ne 's{^\s*//...}{}gm; count /\w+\.Plan\s*=[^=]/
P27_PLAN_YAZAN=$(find "$API" -name '*.cs' ! -name '*.mutorj' 2>/dev/null \
  | grep -v '/obj/\|/bin/\|/Migrations/\|AbonelikServisi\.cs' \
  | xargs perl -0777 -ne 's{^\s*//[^\n]*}{}gm; s{///[^\n]*}{}g;
      $n++ while /\w+\.Plan\s*=[^=]/g; END{print $n+0}' 2>/dev/null | tr -d ' ')
if [ "${P27_PLAN_YAZAN:-9}" = 0 ]; then
  p27_ok "P-27: YETKISIZ_PLAN_YAZAN_UC" "0 (tek yazan: AbonelikServisi)"
else
  p27_kir "P-27: YETKISIZ_PLAN_YAZAN_UC (beklenen 0)" \
    "$P27_PLAN_YAZAN — plan kapısını atlayan ikinci yazma yolu açılmış"
fi

# ── 1b) Kaldırılan yol GERİ GELMESİN: AdminServisi.PlanGuncelle = 0 ─────────
# P-27 bu metodu kaldırdı; geri eklenirse kapı yine iki yerden geçer.
P27_ADMIN_PLAN=$(perl -0777 -ne 's{^\s*//[^\n]*}{}gm; s{///[^\n]*}{}g;
  $n++ while /Task<TenantDto>\s+PlanGuncelle\s*\(/g; END{print $n+0}' \
  "$API/Modules/Admin/AdminModulu.cs" 2>/dev/null | tr -d ' ')
# Controller'daki devreden uç (1) meşrudur; servis metodu (2. eşleşme) değildir.
if [ "${P27_ADMIN_PLAN:-9}" -le 1 ]; then
  p27_ok "P-27: ADMIN_SERVIS_PLAN_YAZAR" "0 (yalnız controller devri)"
else
  p27_kir "P-27: ADMIN_SERVIS_PLAN_YAZAR (beklenen tek devir)" \
    "$P27_ADMIN_PLAN — AdminServisi yeniden plan yazıyor"
fi

# ── 2) PLAN_DEGISIMI_IZSIZ = 0 ──────────────────────────────────────────────
# KURAL: dönem değiştiren her çağrı, AYNI dosyada bir denetim izi damgası
#   (PLAN_DEGISIMI) ile eşleşir. Çağrı sayısı iz sayısını AŞARSA izsiz bir
#   değişim yolu doğmuştur.
# ÖLÇÜM (TAZE, 2026-09-22): çağrı 2 (kiracı + sistem) · iz 2 → fark 0.
P27_DOSYA="$API/Modules/Abonelik/AbonelikServisi.cs"
# Yalnız ÇAĞRI sahaları sayılır (`... = [AbonelikServisi.]DonemDegistir(`);
# metodun TANIMI ("internal static KiraciAbonelik DonemDegistir(") sayılmaz.
P27_CAGRI=$(perl -0777 -ne 's{^\s*//[^\n]*}{}gm; s{///[^\n]*}{}g;
  $n++ while /=\s*(?:AbonelikServisi\.)?DonemDegistir\s*\(/g; END{print $n+0}' \
  "$P27_DOSYA" 2>/dev/null | tr -d ' ')
P27_IZ=$(perl -0777 -ne 's{^\s*//[^\n]*}{}gm; s{///[^\n]*}{}g;
  $n++ while /IzDamgasi\}:/g; END{print $n+0}' "$P27_DOSYA" 2>/dev/null | tr -d ' ')
P27_IZSIZ=$(( P27_CAGRI - ${P27_IZ:-0} ))
[ "$P27_IZSIZ" -lt 0 ] && P27_IZSIZ=0
if [ "$P27_IZSIZ" = 0 ] && [ "$P27_CAGRI" -ge 2 ]; then
  p27_ok "P-27: PLAN_DEGISIMI_IZSIZ" "0 (çağrı $P27_CAGRI · iz $P27_IZ)"
else
  p27_kir "P-27: PLAN_DEGISIMI_IZSIZ (beklenen 0)" \
    "izsiz=$P27_IZSIZ (çağrı $P27_CAGRI · iz $P27_IZ) — nüfus 0 ise sayaç boşa düşmüş"
fi

# ── 3) ABONELIK_FIYAT_KOLONU = 0 (K-16 · MÜHÜR 2) ──────────────────────────
# Rakamlı fiyat KURUCU kalemidir: katalogda tutar/ücret kolonu doğmaz, ürün
# "fiyatı var" izlenimi vermez. Kolon eklenirse bu kapı bilerek kırılır.
P27_FIYAT=$(perl -0777 -ne 's{^\s*//[^\n]*}{}gm; s{///[^\n]*}{}g;
  $n++ while /Column\("(ucret|fiyat|tutar|aylik_ucret)"\)/g; END{print $n+0}' \
  "$API/Modules/Abonelik/AbonelikVarliklari.cs" 2>/dev/null | tr -d ' ')
if [ "${P27_FIYAT:-9}" = 0 ]; then
  p27_ok "P-27: ABONELIK_FIYAT_KOLONU" "0 (K-16 kurucuda)"
else
  p27_kir "P-27: ABONELIK_FIYAT_KOLONU (beklenen 0)" \
    "$P27_FIYAT — fiyat ilanı K-16 ile kurucudadır, şemaya rakam girmez"
fi

# <<< P-27

# >>> P-28 — F-4 KİRACI ETİKETLİ TELEMETRİ + DESTEK DAKİKASI (R-2) · CIRCIR
#
# İKİ KUSURUN GERİ GELME YOLU KAPATILIR:
#   (a) telemetri bir ERİŞİM GÜNLÜĞÜNE dönüşür (yol/başlık/IP/kullanıcı) →
#       Mehmet vetosu (BK-12): kişisel veri taşıyan telemetri TOPLANMAZ.
#   (b) F-8 birim ekonomi belgesine ÖLÇÜLMEMİŞ rakam yazılır → FAIL-CLOSED 1
#       sessizce açılır ve ürün ölçülmemiş rakamla fiyatlanır (BK-49).
p28_ok()  { printf "  ✓ %-46s %s\n" "$1" "$2"; }
p28_kir() { printf "  ✗ %-46s %s\n" "$1" "$2"; HATA=1; }

# ── 1) TELEMETRIDE_KISISEL_VERI = 0 ────────────────────────────────────────
# KAPSAM: OTOMATİK toplama yolu (ara katman + toplayıcı + yazıcı). Operatörün
#   BİLİNÇLİ destek kaydı (DestekDakikasi.Aciklama) bu kapsamda DEĞİLDİR —
#   o alan otomatik toplanmaz, elle girilir ve denetim izine düşer.
# ÖLÇÜM (TAZE, 2026-09-23 — sonuç 0; yorumlar SÖKÜLEREK sayılır):
#   perl -0777 -ne 's{^\s*//...}{}gm; count /Request\.|RemoteIpAddress|ClaimTypes|
#     FindFirst\("|\.Identity|Cookies|Headers\[/  →  Araci 0 · Toplayici 0 · Yazici 0
# NOT: TelemetriAraci kiracı ETİKETİNİ `FindFirst(KiraciClaim)` ile okur —
#   sabit ile. `FindFirst("...")` (düz dizge) yeni bir claim okumak demektir
#   ve bilerek YASAKTIR: etiket dışında kimlik okunamaz.
P28_TEL_DIZIN="$API/Modules/Telemetri"
P28_KV=0
for f in "$P28_TEL_DIZIN/TelemetriAraci.cs" "$P28_TEL_DIZIN/TelemetriToplayici.cs" \
         "$P28_TEL_DIZIN/TelemetriYazici.cs"; do
  [ -f "$f" ] || continue
  n=$(perl -0777 -ne 's{^\s*//[^\n]*}{}gm; s{///[^\n]*}{}g;
    $n++ while /Request\.|RemoteIpAddress|ClaimTypes|FindFirst\("|\.Identity|Cookies|Headers\[/g;
    END{print $n+0}' "$f" 2>/dev/null | tr -d ' ')
  P28_KV=$((P28_KV + ${n:-0}))
done
# Şema tarafı: ölçüm tablosunda kişisel veri kolonu DOĞAMAZ.
P28_KOLON=$(perl -0777 -ne 's{^\s*//[^\n]*}{}gm; s{///[^\n]*}{}g;
  $n++ while /Column\("(ip|ip_adresi|kullanici|kullanici_id|email|eposta|telefon|yol|path|url|sorgu|basliklar|govde|cari_id|urun_id)"\)/g;
  END{print $n+0}' "$P28_TEL_DIZIN/TelemetriVarliklari.cs" 2>/dev/null | tr -d ' ')
P28_KV=$((P28_KV + ${P28_KOLON:-0}))
if [ ! -d "$P28_TEL_DIZIN" ]; then
  p28_kir "P-28: TELEMETRIDE_KISISEL_VERI" "Modules/Telemetri YOK — sayaç boşa düşmüş"
elif [ "$P28_KV" = 0 ]; then
  p28_ok "P-28: TELEMETRIDE_KISISEL_VERI" "0 (ara katman + toplayıcı + yazıcı + şema)"
else
  p28_kir "P-28: TELEMETRIDE_KISISEL_VERI (beklenen 0)" \
    "$P28_KV — telemetri erişim günlüğüne dönüyor (BK-12 · Mehmet vetosu)"
fi

# ── 2) F8_OLCULMEMIS_SATIR — yalnız AZALIR (tavan çırçırı) ──────────────────
# KURAL: F-8 birim ekonomi belgesinde "OLCULMEDI" damgalı satır sayısı
#   ARTAMAZ. Bugün 2'dir (destek dakikası · kiracı başı kaynak tüketimi);
#   P-25 (k6) ve P-32 (pilot) gerçek rakamı getirince 0'a iner ve tavan da
#   o zaman düşürülür. Yeni bir ölçülmemiş satır açmak bu kapıyı kırar.
# ÖLÇÜM (TAZE, 2026-09-23): grep -c "| OLCULMEDI |" F-8-BIRIM-EKONOMI-...md → 2
P28_F8="../dokumanlar/02-mimari/F-8-BIRIM-EKONOMI-2026-09-16.md"
P28_F8_TAVAN=2
if [ ! -f "$P28_F8" ]; then
  p28_kir "P-28: F8_OLCULMEMIS_SATIR" "F-8 belgesi YOK — sayaç boşa düşmüş"
else
  P28_F8_SATIR=$(grep -c "| OLCULMEDI |" "$P28_F8" | tr -d ' ')
  if [ "$P28_F8_SATIR" -gt "$P28_F8_TAVAN" ]; then
    p28_kir "P-28: F8_OLCULMEMIS_SATIR (tavan $P28_F8_TAVAN)" \
      "$P28_F8_SATIR — ölçülmemiş satır ARTTI, tavan yalnız azalır"
  elif [ "$P28_F8_SATIR" = 0 ]; then
    p28_ok "P-28: F8_OLCULMEMIS_SATIR" "0 (Faz B kapandı)"
  else
    # FAIL-CLOSED 1: ölçülmemiş satır varken fiyat kilidi belgede YAZILI olmalı.
    if grep -q "FAIL-CLOSED 1 AKTİF" "$P28_F8"; then
      p28_ok "P-28: F8_OLCULMEMIS_SATIR" \
        "$P28_F8_SATIR/$P28_F8_TAVAN (fiyat ilanı KİLİTLİ — P-25/P-32 doldurur)"
    else
      p28_kir "P-28: F8_OLCULMEMIS_SATIR = $P28_F8_SATIR ama kilit YAZILI DEĞİL" \
        "FAIL-CLOSED 1 ibaresi F-8'den düşmüş — ölçülmeden fiyat ilan edilebilir (BK-49)"
    fi
  fi
fi

# ── 3) TELEMETRI_ELLE_F8_RAKAMI = 0 ────────────────────────────────────────
# Telemetri kodu MİKTAR ölçer (dakika, istek, bayt). Saatlik ücret / TL tutar
# sabiti koda girerse F-8'in rakamı koddan doğar ve K-16 (fiyat ilanı kurucuda)
# delinir. ÖLÇÜM (TAZE, 2026-09-23 → 0): Modules/Telemetri altında TL/ücret
# sabiti ya da Column("...ucret|fiyat|tutar|maliyet...") aranır.
P28_FIYAT=$(perl -0777 -ne 's{^\s*//[^\n]*}{}gm; s{///[^\n]*}{}g;
  $n++ while /Column\("[^"]*(ucret|fiyat|tutar|maliyet)[^"]*"\)|decimal\s+(Ucret|Fiyat|Tutar|Maliyet)\b/g;
  END{print $n+0}' "$P28_TEL_DIZIN"/*.cs 2>/dev/null | tr -d ' ')
if [ "${P28_FIYAT:-9}" = 0 ]; then
  p28_ok "P-28: TELEMETRI_ELLE_F8_RAKAMI" "0 (miktar ölçülür, TL kurucuda — K-16)"
else
  p28_kir "P-28: TELEMETRI_ELLE_F8_RAKAMI (beklenen 0)" \
    "$P28_FIYAT — telemetriye para kolonu girmiş, F-8 rakamı koddan doğuyor"
fi

# <<< P-28

# >>> P-29 — S-1 TİCARİ PAKET + ÜRÜN ANLATIMI (R-3 · R-8 · D-8) · CIRCIR
#
# ÜÇ KUSURUN GERİ GELME YOLU KAPATILIR:
#   (a) kısıt beyanı İKİNCİ bir yerde elle yazılır → iki metin ayrışır ve biri
#       müşteriye yanlış söz verir (BK-26 · Köprü K-1). Tek kaynak:
#       docs/HARIC-LISTESI.md.
#   (b) S-1 belgesi "TASLAK — ilan edilmemiştir" damgasını kaybeder → ölçülmemiş
#       rakamla dışarıya çıkar (BK-49 · K-16 kurucu imzası delinir).
#   (c) S-1 metnine tek rakamlı tasarruf / silme oranı ya da kendi fiyatımız
#       girer → R-8 kararı ve FAIL-CLOSED 1 delinir.
p29_ok()  { printf "  ✓ %-46s %s\n" "$1" "$2"; }
p29_kir() { printf "  ✗ %-46s %s\n" "$1" "$2"; HATA=1; }

P29_DOCS="../docs"
P29_KAYNAK="$P29_DOCS/HARIC-LISTESI.md"
P29_BELGELER="HARIC-LISTESI.md SLA.md HIZLI-BASLANGIC.md URUN-ANLATIMI.md"

# ── 1) HARIC_KAYNAK_BOZUK = 0 ──────────────────────────────────────────────
# Tek kaynak dosyası DURMALI ve içinde (i) H-1..H-8 kısıt satırları,
# (ii) GÖÇ KARARI bölümü (BK-31 — sessiz bırakılamaz) bulunmalı.
# ÖLÇÜM (TAZE, 2026-09-23): grep -c '^| H-[1-8] ' → 8 · grep -c 'GÖÇ KARARI' → 1
P29_BOZUK=0
if [ ! -f "$P29_KAYNAK" ]; then
  P29_BOZUK=1
  p29_kir "P-29: HARIC_KAYNAK_BOZUK" "docs/HARIC-LISTESI.md YOK — kısıt beyanının tek kaynağı düşmüş"
else
  P29_H=$(grep -c '^| H-[1-8] ' "$P29_KAYNAK" | tr -d ' ')
  P29_GOC=$(grep -c 'GÖÇ KARARI' "$P29_KAYNAK" | tr -d ' ')
  if [ "$P29_H" -lt 8 ] || [ "$P29_GOC" -lt 1 ]; then
    P29_BOZUK=1
    p29_kir "P-29: HARIC_KAYNAK_BOZUK (beklenen 0)" \
      "H satırı $P29_H/8 · göç kararı $P29_GOC/1 — BK-31 sessiz bırakılamaz"
  else
    p29_ok "P-29: HARIC_KAYNAK_BOZUK" "0 (8 kısıt satırı + göç kararı yerinde)"
  fi
fi

# ── 2) HARIC_BEYAN_KOPYA — tavan yalnız AZALIR ─────────────────────────────
# KAPSAM: docs/*.md (müşteriye giden belge hattı). Kaynak dosyanın kendisi ve
#   HARIC-KAYNAK işaretini taşıyan (yani kaynağı GÖMEREK kullanan) belgeler
#   sayılmaz. Ürün ekranı / demo giriş ekranı tarafı BK-47 gereği P-30/P-32'nin
#   PILOT_KISIT_BEYANSIZ_DAVET sayacındadır — burada İKİNCİ KEZ sayılmaz.
# ÖLÇÜM (TAZE, 2026-09-23 → 2): aşağıdaki döngünün aynısı; bulunanlar
#   docs/KULLANIM_KILAVUZU.md:396 ve docs/MIMARI.md:369 (ikisi de "yol
#   haritası" biçiminde ikinci kısıt beyanı). İKİSİ DE P-33'ÜN TEK ELİNDEDİR
#   (plan § 7: "docs/ + kılavuz P-33 tek eldir; P-29 ayrı dosyalara yazar").
#   Bu yüzden P-29 onları düzeltemez → HEDEF 0, BUGÜN 2, devir: P-33.
#   TAVAN ARTMAZ: yeni bir elle yazılmış kısıt beyanı bu kapıyı kırar.
# PAKET P-33 (devir kapandı, 2026-09-23): KULLANIM_KILAVUZU.md artık HARİÇ
#   listesini GÖMÜYOR, MIMARI.md'deki "yol haritası" cümlesi HARIC-KAYNAK
#   işaretiyle değiştirildi → TAZE ölçüm 0. Cırcır: 2 → 0.
P29_KOPYA_TAVAN=0
P29_DESEN='(HARİÇ|HARIÇ|HARIC|yol haritas|kapsam dışı|kapsam disi)[^|]{0,80}(e-Fatura|e-Arşiv|ÖKC|SMS|WhatsApp|banka API|açık bankacılık|imalat|RFID)|(e-Fatura|e-Arşiv|ÖKC|SMS|WhatsApp|banka API|açık bankacılık|imalat|RFID)[^|]{0,80}(HARİÇ|HARIÇ|HARIC|yol haritas|kapsam dışı|kapsam disi)'
P29_KOPYA=0
P29_KOPYA_AD=""
for f in "$P29_DOCS"/*.md; do
  [ -f "$f" ] || continue
  case "$(basename "$f")" in HARIC-LISTESI.md) continue;; esac
  grep -q 'HARIC-KAYNAK: docs/HARIC-LISTESI.md' "$f" && continue
  n=$(grep -icE "$P29_DESEN" "$f" | tr -d ' ')
  if [ "${n:-0}" -gt 0 ]; then
    P29_KOPYA=$((P29_KOPYA + 1))
    P29_KOPYA_AD="$P29_KOPYA_AD $(basename "$f")"
  fi
done
if [ "$P29_KOPYA" -gt "$P29_KOPYA_TAVAN" ]; then
  p29_kir "P-29: HARIC_BEYAN_KOPYA (tavan $P29_KOPYA_TAVAN)" \
    "$P29_KOPYA —$P29_KOPYA_AD · ikinci elle yazılmış kısıt beyanı (BK-26)"
elif [ "$P29_KOPYA" = 0 ]; then
  p29_ok "P-29: HARIC_BEYAN_KOPYA" "0 (kısıt beyanı tek kaynaktan)"
else
  p29_ok "P-29: HARIC_BEYAN_KOPYA" \
    "$P29_KOPYA/$P29_KOPYA_TAVAN miras —$P29_KOPYA_AD (hedef 0 · devir P-33)"
fi

# ── 3) S1_BELGE_DAMGASIZ = 0 ───────────────────────────────────────────────
# Dört S-1 belgesi DURMALI ve hepsi "TASLAK — ilan edilmemiştir" damgasını
# taşımalı. ÖLÇÜM (TAZE, 2026-09-23): 4/4 damgalı → 0.
P29_DAMGASIZ=0
# ⚠ zsh: ${=VAR} olmadan bölmez → dört belge TEK ad sanılıp YANLIŞ KIRMIZI
#   veriyordu (P-30 doğrulamasında saptandı; eşik/tavan değişmedi).
for b in $(echo "$P29_BELGELER"); do
  if [ ! -f "$P29_DOCS/$b" ]; then
    P29_DAMGASIZ=$((P29_DAMGASIZ + 1))
  elif ! grep -q 'TASLAK — ilan edilmemiştir' "$P29_DOCS/$b"; then
    P29_DAMGASIZ=$((P29_DAMGASIZ + 1))
  fi
done
if [ "$P29_DAMGASIZ" = 0 ]; then
  p29_ok "P-29: S1_BELGE_DAMGASIZ" "0 (4 belge · TASLAK damgası yerinde)"
else
  p29_kir "P-29: S1_BELGE_DAMGASIZ (beklenen 0)" \
    "$P29_DAMGASIZ — damgasız/eksik S-1 belgesi (BK-49 · K-16)"
fi

# ── 4) S1_RAKAM_ILANI = 0 (R-8) ────────────────────────────────────────────
# S-1 belgelerinde tasarruf/silme oranı ya da kendi fiyatımız İDDİA EDİLEMEZ.
# Olumsuzlama taşıyan satırlar (ilan edilmez · yok · doğrulanamadı · iddia ·
# içermez · TASLAK) kural metnidir, iddia değildir; sayılmaz.
# ÖLÇÜM (TAZE, 2026-09-23 → 0):
#   grep -inE 'tasarruf|silme oran|daha ucuz|%[[:space:]]?[0-9]+' <4 belge> |
#   grep -viE 'edilmez|edilemez|edilmemiş|yok|kullanılmaz|doğrulanam|ilan edil|içermez|iddia|TASLAK'
P29_RAKAM=0
# ⚠ zsh: ${=VAR} olmadan bölmez → döngü tek (var olmayan) ada düşüp her zaman
#   0 veriyordu, yani sayaç YANLIŞ YEŞİLDİ. (P-30 doğrulamasında saptandı.)
for b in $(echo "$P29_BELGELER"); do
  [ -f "$P29_DOCS/$b" ] || continue
  n=$(grep -inE 'tasarruf|silme oran|daha ucuz|%[[:space:]]?[0-9]+' "$P29_DOCS/$b" \
      | grep -vicE 'edilmez|edilemez|edilmemiş|yok|kullanılmaz|doğrulanam|ilan edil|içermez|iddia|TASLAK' | tr -d ' ')
  P29_RAKAM=$((P29_RAKAM + ${n:-0}))
done
if [ "$P29_RAKAM" = 0 ]; then
  p29_ok "P-29: S1_RAKAM_ILANI" "0 (tek rakamlı tasarruf/fiyat iddiası yok — R-8)"
else
  p29_kir "P-29: S1_RAKAM_ILANI (beklenen 0)" \
    "$P29_RAKAM — S-1 metninde rakamlı iddia (R-8 · FAIL-CLOSED 1 · K-16)"
fi

# ── 5) SLA_OLCULMEMIS_RAKAM = 0 ────────────────────────────────────────────
# KUSUR (doğrulayıcı, 2026-09-23 · KRİTİK): docs/SLA.md § 1 "TAAHHÜT EDİLEBİLİR
# (kanıtlı)" tablosunun kanıt sütununa, hiç ölçülmeden, görev metnindeki eski
# değişmez cümlesinden kopyalanmış iki rakam yazılmıştı (S-1 "IgnoreQueryFilters
# beyaz listede 4" → GERÇEK 0 · S-3 "ExecuteDelete beyaz listesi 4" → GERÇEK 5).
# Belge doğrudan müşteriye gidiyor; DoD "kanıtı olmayan sayı yazılmaz" +
# Anayasa md.1 ihlali. Dört P-29 sayacının hiçbiri rakamların DEĞERİNE bakmıyordu.
# BU SAYAÇ: § 1'deki HER kalın rakam ya taze ölçülür ya canlı-koşum izinlisidir.
# ÖLÇÜM (TAZE, 2026-09-23):
#   IQF   : grep -rh "\.IgnoreQueryFilters()" --include='*.cs' Kuyumcu.Api | grep -c .   → 0
#   BEYAZ : grep -n 'denetle "ExecuteUpdate/Delete' "$BETIK_YOLU" | head -1 |
#           grep -oE '[0-9]+$'   → 5   (SATIR NUMARASI YAZILMAZ: betiğe blok eklenince
#           kayar; desenle ölçülür — doğrulayıcı 2026-09-23 bulgusu)
#   § 1 kalın rakamlı satırlar: S-1, S-3, S-4 (S-4 canlı koşum — izinli, tarihli)
P29_SLA="$P29_DOCS/SLA.md"
P29_SLA_OLCULMEMIS=0
if [ ! -f "$P29_SLA" ]; then
  P29_SLA_OLCULMEMIS=1
  P29_SLA_AYRINTI="docs/SLA.md YOK"
else
  P29_SLA_B1=$(awk '/^## 1\. TAAHHÜT/{f=1} f&&/^## 2\./{f=0} f' "$P29_SLA")
  # taze ölçümler
  P29_OLC_IQF=$(grep -rh "\.IgnoreQueryFilters()" --include='*.cs' "$API" 2>/dev/null | grep -c .)
  P29_OLC_BEYAZ=$(grep -n 'denetle "ExecuteUpdate/Delete' "$BETIK_YOLU" | head -1 | grep -oE '[0-9]+$')
  P29_SLA_AYRINTI=""
  while IFS= read -r satir; do
    case "$satir" in '| S-'*) ;; *) continue ;; esac
    p29_sno=$(printf '%s' "$satir" | grep -oE '^\| S-[0-9]+' | grep -oE 'S-[0-9]+')
    p29_rakamlar=$(printf '%s' "$satir" | grep -oE '\*\*[0-9]+' | tr -d '*')
    [ -n "$p29_rakamlar" ] || continue
    for r in $p29_rakamlar; do
      bekle=""
      case "$p29_sno" in
        S-1)  bekle="$P29_OLC_IQF" ;;
        S-3)  bekle="$P29_OLC_BEYAZ" ;;
        S-4)  # canlı koşum sayımı: yeniden ölçülemez → tarih + "Canlı koşum" ŞART
              if printf '%s' "$satir" | grep -q 'Canlı koşum (20'; then
                continue
              else
                bekle="(canlı koşum etiketi+tarihi eksik)"
              fi ;;
        *)    bekle="(kayıtsız — ölçüm komutu tanımlı değil)" ;;
      esac
      # Ölçüm BOŞ dönmüşse bu "eşit değil" değil, "ÖLÇÜLEMEDİ"dir: nedeniyle kır.
      if [ -z "$bekle" ]; then
        P29_SLA_OLCULMEMIS=$((P29_SLA_OLCULMEMIS + 1))
        P29_SLA_AYRINTI="$P29_SLA_AYRINTI $p29_sno:yazılan=$r/ÖLÇÜLEMEDİ(betik kendi yolundan okunamadı ya da ölçüm boş — kapı kapalı)"
      elif [ "$r" != "$bekle" ]; then
        P29_SLA_OLCULMEMIS=$((P29_SLA_OLCULMEMIS + 1))
        P29_SLA_AYRINTI="$P29_SLA_AYRINTI $p29_sno:yazılan=$r/ölçülen=$bekle"
      fi
    done
  done <<EOF
$P29_SLA_B1
EOF
fi
if [ "$P29_SLA_OLCULMEMIS" = 0 ]; then
  p29_ok "P-29: SLA_OLCULMEMIS_RAKAM" "0 (§1 kalın rakamların hepsi taze ölçümle eşleşti)"
else
  p29_kir "P-29: SLA_OLCULMEMIS_RAKAM (beklenen 0)" \
    "$P29_SLA_OLCULMEMIS —$P29_SLA_AYRINTI (DoD: kanıtı olmayan sayı yazılmaz)"
fi

# ── 6) SLA_BAYAT_SATIR_GOSTERGESI = 0 ──────────────────────────────────────
# KUSUR (doğrulayıcı, 2026-09-23 · ORTA): docs/SLA.md müşteriye giden belgede
# kanıt sütunu "denetim.sh:65" gibi SATIR NUMARASI veriyordu; P-30b betiğe blok
# ekleyince gerçek satır 88 oldu → belge kendi kanıtını yanlış yere gösteriyor.
# Satır numarası izlenebilirlik değil, çürüyen göstergedir. Kural: denetim.sh'a
# yapılan atıf DESENLE verilir (hangi sayaç satırı), numarayla değil.
# ÖLÇÜM (TAZE, 2026-09-23): grep -oE 'denetim\.sh:[0-9]+' docs/SLA.md | wc -l → 0
if [ -f "$P29_SLA" ]; then
  P29_SLA_SATIRGOST=$(grep -oE 'denetim\.sh:[0-9]+' "$P29_SLA" | wc -l | tr -d ' ')
else
  P29_SLA_SATIRGOST=0
fi
if [ "$P29_SLA_SATIRGOST" = 0 ]; then
  p29_ok "P-29: SLA_BAYAT_SATIR_GOSTERGESI" "0 (denetim.sh atıfları desenle veriliyor)"
else
  p29_kir "P-29: SLA_BAYAT_SATIR_GOSTERGESI (beklenen 0)" \
    "$P29_SLA_SATIRGOST — SLA.md'de denetim.sh satır numarası (blok eklenince kayar; desen ver)"
  grep -nE 'denetim\.sh:[0-9]+' "$P29_SLA" | cut -c1-110 | sed 's/^/      → /'
fi

# <<< P-29

# NOT (dogrulama turu 3, 2026-09-23): bu betik ./denetim.sh olarak BASH
# shebang'iyle de kosuluyor (.github/workflows/ci.yml:79). zsh'e ozgu
# ${=VAR} bolme sozdizimi BASH'te "bad substitution" verir ve kontrol
# SESSIZCE ATLANIR (fail-open). Bu yuzden kelime bolme her yerde iki
# kabukta da calisan $(echo "$VAR") kalibiyla yazilir.
# >>> P-30 — DEMO-1 TEMİZ DEMO KİRACISI + 15 DK GÖSTERİM (R-4) · CIRCIR
#
# ÜÇ KUSURUN GERİ GELME YOLU KAPATILIR:
#   (a) demo kiracısı QA fikstüründen türetilir ya da toplu temizlikle
#       süpürülür → gösterim bir sabah "kiracı yok" diye açılmaz (P-30 DoD).
#   (b) gösterimin dokunduğu rota sessizce bozulur → demo canlıda patlar
#       (BK-46: iki ardışık turda kritik = 0).
#   (c) giriş ekranındaki kısıt beyanı ile docs/HARIC-LISTESI.md ayrışır →
#       müşteriye iki farklı söz verilir (BK-47 · BK-26).
p30_ok()  { printf "  ✓ %-46s %s\n" "$1" "$2"; }
p30_kir() { printf "  ✗ %-46s %s\n" "$1" "$2"; HATA=1; }
p30_uyar(){ printf "  ⚠ %-46s %s\n" "$1" "$2"; }

P30_BETIK="../dokumanlar/09-uygulama-kayitlari/betikler/demo-tohumla.js"
P30_TEMIZLE="../dokumanlar/09-uygulama-kayitlari/betikler/kiraci-temizle.sh"
P30_HAM="../dokumanlar/09-uygulama-kayitlari/ham"
P30_RUNBOOK="../docs/RUNBOOK-DEMO.md"
P30_SOZLESME="Kuyumcu.Contracts/Kisit/KisitSozlesmeleri.cs"
P30_KAYNAK="../docs/HARIC-LISTESI.md"

# ── 1) DEMO_IZOLASYON = 0 ──────────────────────────────────────────────────
# ÖLÇÜM (TAZE, 2026-09-23): aşağıdaki beş denetimin hepsi geçiyor → 0.
#   test -f betik/runbook · grep 'DEMO-\*)' temizle.sh · grep 'QA-TEST-' betik
#   · grep -icE 'INSERT INTO|docker exec|mysql -u' betik → 0
P30_IZO=0
P30_IZO_AYRINTI=""
[ -f "$P30_BETIK" ]   || { P30_IZO=$((P30_IZO+1)); P30_IZO_AYRINTI="$P30_IZO_AYRINTI demo-tohumla.js-YOK"; }
[ -f "$P30_RUNBOOK" ] || { P30_IZO=$((P30_IZO+1)); P30_IZO_AYRINTI="$P30_IZO_AYRINTI RUNBOOK-DEMO.md-YOK"; }
if [ -f "$P30_TEMIZLE" ]; then
  grep -q 'DEMO-\*)' "$P30_TEMIZLE" \
    || { P30_IZO=$((P30_IZO+1)); P30_IZO_AYRINTI="$P30_IZO_AYRINTI temizle.sh-DEMO-reddi-yok"; }
else
  P30_IZO=$((P30_IZO+1)); P30_IZO_AYRINTI="$P30_IZO_AYRINTI kiraci-temizle.sh-YOK"
fi
if [ -f "$P30_BETIK" ]; then
  grep -q 'QA-TEST-' "$P30_BETIK" \
    || { P30_IZO=$((P30_IZO+1)); P30_IZO_AYRINTI="$P30_IZO_AYRINTI betik-QA-fikstur-reddi-yok"; }
  # Doğrudan veritabanı yazımı: demo YALNIZ ürünün uçlarından tohumlanır.
  P30_SQL=$(grep -icE 'INSERT INTO|docker exec|mysql -u' "$P30_BETIK" | tr -d ' ')
  [ "${P30_SQL:-0}" = 0 ] \
    || { P30_IZO=$((P30_IZO+1)); P30_IZO_AYRINTI="$P30_IZO_AYRINTI betikte-dogrudan-SQL($P30_SQL)"; }
  # KALICI YASAK (doğrulayıcı bulgusu, 2026-09-23): "3 aylık geçmiş" hiçbir
  # zaman SQL ile tarih kaydırarak ÜRETİLMEZ. Kaydırılmış tarih, denetim damga
  # zincirini ve mutabakat kayıtlarını o an yalancı yapar (Anayasa md.1).
  # Geçmişe tarihli tohumlama YALNIZ uçlara birinci sınıf "işlem tarihi"
  # alanı eklenerek yapılır (devredildi: ayrı paket).
  # ÖLÇÜM (TAZE, 2026-09-23): 0 eşleşme.
  P30_TARIH=$(grep -icE 'DATE_SUB|DATE_ADD|INTERVAL[[:space:]]+[0-9]+[[:space:]]*(DAY|MONTH|YEAR)|UPDATE[[:space:]]+[a-z_]+[[:space:]]+SET' "$P30_BETIK" | tr -d ' ')
  [ "${P30_TARIH:-0}" = 0 ] \
    || { P30_IZO=$((P30_IZO+1)); P30_IZO_AYRINTI="$P30_IZO_AYRINTI betikte-SQL-tarih-kaydirma($P30_TARIH)"; }
  # Provayı koşan ajanın kimliği fail-closed alınmalı (BK-45).
  grep -q -- '--kosan' "$P30_BETIK" \
    || { P30_IZO=$((P30_IZO+1)); P30_IZO_AYRINTI="$P30_IZO_AYRINTI betikte-kosan-kimligi-yok"; }
fi
if [ "$P30_IZO" = 0 ]; then
  p30_ok "P-30: DEMO_IZOLASYON" "0 (demo QA fikstüründen türemez, toplu temizlik süpüremez, doğrudan SQL yok)"
else
  p30_kir "P-30: DEMO_IZOLASYON (beklenen 0)" "$P30_IZO —$P30_IZO_AYRINTI"
fi

# ── 2) DEMO_ROTA_KRITIK = 0 (BK-46) ────────────────────────────────────────
# En yeni İKİ prova dosyasının İKİSİNDE de her adım kritik = 0 olmalı.
# Tek dosya kanıt sayılmaz (BK-45: "tek koşum kanıt sayılmaz").
# ÖLÇÜM (TAZE, 2026-09-23): 2 dosya, her ikisinde de kritik toplamı 0.
P30_PROVALAR=$(ls -1t "$P30_HAM"/demo-prova-*.json 2>/dev/null | head -2)
P30_PROVA_SAYI=$(printf '%s\n' "$P30_PROVALAR" | grep -c 'demo-prova-' | tr -d ' ')
if [ "${P30_PROVA_SAYI:-0}" -lt 2 ]; then
  p30_kir "P-30: DEMO_ROTA_KRITIK (beklenen 0)" \
    "prova dosyası $P30_PROVA_SAYI/2 — iki ardışık tur kanıtı yok (BK-45/BK-46)"
else
  # BK-45 SERTLEŞTİRMESİ (doğrulayıcı bulgusu, 2026-09-23): eski sayaç AYNI
  # ajanın 11 saniye arayla koştuğu iki dosyayı YEŞİL veriyordu. Artık son iki
  # provanın "kosan" alanı DOLU ve BİRBİRİNDEN FARKLI olmalı; her adım
  # başlangıç/bitiş damgası taşımalı. Tek ajanın iki koşumu kanıt değildir.
  # ÖLÇÜM: python3 ile son iki demo-prova-*.json okunur.
  P30_KRITIK=$(python3 - "$P30_HAM" <<'PYEOF'
import glob, json, os, sys
ham = sys.argv[1]
dosyalar = sorted(glob.glob(os.path.join(ham, 'demo-prova-*.json')), key=os.path.getmtime)[-2:]
toplam = 0
kosanlar = []
for d in dosyalar:
    try:
        veri = json.load(open(d, encoding='utf-8'))
    except Exception:
        print('999 OKUNAMAYAN'); raise SystemExit      # okunamayan prova = kanıt değil
    adimlar = veri.get('adimlar') or []
    if not adimlar:
        toplam += 999
    toplam += sum(int(a.get('kritik', 1)) for a in adimlar)
    # her adım başlangıç/bitiş damgası taşımalı (süre iddiası denetlenebilsin)
    toplam += sum(1 for a in adimlar if not a.get('basladi') or not a.get('bitti'))
    # İNSAN SÜRESİ BURADA ÖLÇÜLMEZ — ayrı kapıdadır (P-30b, BK-45).
    # KALDIRILAN FAIL-OPEN: 'toplam += int(veri.get("insanDkAsildi", 0) or 0)'
    # bayrak YOKSA 0 sayıyordu; ham provaların hiçbirinde bayrak yoktu, yani
    # "20 dakikayı aşarsa paket kapanmaz" kuralı HİÇ ÖLÇÜLMEDEN yeşil veriyordu.
    kosanlar.append((veri.get('kosan') or '').strip())
if any(not k for k in kosanlar):
    print('%d KOSAN-DAMGASIZ' % (toplam + 1)); raise SystemExit
if len(set(kosanlar)) < 2:
    print('%d TEK-AJAN(%s)' % (toplam + 1, kosanlar[0])); raise SystemExit
print('%d TAMAM' % toplam)
PYEOF
)

  # ROTA VARLIK KONTROLU (dogrulama turu 3, 2026-09-23): runbook "2. 15 dakikalik
  # gosterim" tablosundaki UI rotalarinin HEPSI Kuyumcu.Web/Pages altinda bir
  # @page olarak bulunmali. GERCEK BULGU: 8. adimda yazili "/vade-takibi" rotasi
  # YOKTU -> canli tarayicida "Sayfa bulunamadi" ciziyordu (dogrusu
  # /raporlar/vade-takibi). Prova betigi API ucunu olctugu icin goremiyordu.
  # OLCUM (TAZE, 2026-09-23): 10 rota, hepsi var -> 0.
  #   grep -oE '^\| [0-9]+ \| [0-9]+ \| `/[a-z0-9/-]*`' ../docs/RUNBOOK-DEMO.md \
  #     | grep -oE '`/[a-z0-9/-]*`' | tr -d '`'
  P30_ROTALAR=$(grep -oE '^\| [0-9]+ \| [0-9]+ \| `/[a-z0-9/-]*`' "$P30_RUNBOOK" 2>/dev/null \
                 | grep -oE '`/[a-z0-9/-]*`' | tr -d '`')
  P30_ROTA_YOK=0; P30_ROTA_AD=""
  for P30_R in $(echo "$P30_ROTALAR"); do
    grep -rqE "^@page \"$P30_R\"" Kuyumcu.Web/Pages 2>/dev/null \
      || { P30_ROTA_YOK=$((P30_ROTA_YOK+1)); P30_ROTA_AD="$P30_ROTA_AD $P30_R"; }
  done
  if [ "${P30_ROTA_YOK:-0}" != 0 ]; then
    P30_KRITIK="$(( ${P30_KRITIK%% *} + P30_ROTA_YOK )) RUNBOOK-ROTASI-YOK($P30_ROTA_AD )"
  fi
  P30_KRITIK_SAYI=${P30_KRITIK%% *}
  P30_KRITIK_NOT=${P30_KRITIK#* }
  if [ "${P30_KRITIK_SAYI:-999}" = 0 ]; then
    p30_ok "P-30: DEMO_ROTA_KRITIK" "0 (son iki prova: iki AYRI ajan · her rota kritiksiz · adım damgalı)"
  else
    p30_kir "P-30: DEMO_ROTA_KRITIK (beklenen 0)" \
      "$P30_KRITIK_SAYI — $P30_KRITIK_NOT (BK-45/BK-46: iki AYRI ajan, iki ardışık temiz tur)"
  fi
fi

# ── 2b) DEMO_INSAN_SURESI · BK-45 (ÜÇ DURUMLU — P-30b fail-open onarımı) ────
# ARIZA (2026-09-23, P-30 hasım doğrulayıcısının KRİTİK bulgusu): insan bütçesi
# kapısı yukarıdaki kritik sayacının içinde "toplam += int(veri.get(
# 'insanDkAsildi', 0) or 0)" satırıyla ölçülüyordu. Bayrak YOKSA 0 kabul
# ediliyordu ve ham/demo-prova-*.json dosyalarının hiçbirinde ölçülmüş bir süre
# yoktu (insanSuresiOlculdu=false) — yani BK-45'in "gösterim 20 dakikayı aşarsa
# paket kapanmaz" kuralı HİÇ ÖLÇÜLMEDEN yeşil veriyordu (fail-open).
# "Yok = 0" YASAKTIR (Anayasa md.1: bilinmeyen tahmin edilmez, doğrulanır).
# ÜÇ DURUM — P-02'deki BK-39 kalıbının (P02-14) aynısı:
#   (1) insanSuresiOlculdu=true  + tavan var + toplam <= tavan  → ✓ temiz ölçüm
#   (2) insanSuresiOlculdu=false AÇIKÇA yazılı + tavan var + toplam <= tavan
#       + son tarih geçmemiş                                    → ⚠ ÜST SINIR (beyan)
#   (3) alan hiç yok · tavan yok · sayı değil · toplam > tavan · son tarih geçti
#                                                                → ✗ DENETİM KIRILIR
# SON TARİH GEVŞETİLEMEZ: beyan süresiz kabul edilemez; kronometreli ölçüm
# (her adımın basladi/bitti damgasından türeyen gerçek insan dakikası) şarttır.
# ÖLÇÜM: python3 ile son iki demo-prova-*.json okunur (kritik sayacıyla aynı iki dosya).
P30_INSAN_SON_TARIH="2026-09-30"   # BK-39 üst sınır son tarihiyle aynı gün
P30_INSAN=$(python3 - "$P30_HAM" <<'PYEOF'
import glob, json, os, sys
ham = sys.argv[1]
dosyalar = sorted(glob.glob(os.path.join(ham, 'demo-prova-*.json')), key=os.path.getmtime)[-2:]
if len(dosyalar) < 2:
    print('KIRIK|prova dosyası %d/2 — insan süresi iki ardışık turdan ölçülemez' % len(dosyalar))
    raise SystemExit

def sayi(v):
    return v if isinstance(v, (int, float)) and not isinstance(v, bool) else None

durumlar, notlar = [], []
for d in dosyalar:
    ad = os.path.basename(d)
    try:
        veri = json.load(open(d, encoding='utf-8'))
    except Exception:
        durumlar.append('KIRIK'); notlar.append('%s OKUNAMADI' % ad); continue
    if 'insanDkToplam' not in veri or 'insanDkTavan' not in veri:
        # "yok = 0" YASAK: ölçümün yokluğu ayrı bir durumdur, sıfır değildir.
        durumlar.append('KIRIK'); notlar.append('%s: insanDkToplam/insanDkTavan YOK — ÖLÇÜLEMEDİ' % ad); continue
    toplam, tavan = sayi(veri.get('insanDkToplam')), sayi(veri.get('insanDkTavan'))
    if toplam is None or tavan is None:
        durumlar.append('KIRIK'); notlar.append('%s: süre alanları sayı değil — ÖLÇÜLEMEDİ' % ad); continue
    if toplam > tavan:
        durumlar.append('KIRIK'); notlar.append('%s: %g dk > tavan %g dk — BK-45 aşıldı' % (ad, toplam, tavan)); continue
    olculdu = veri.get('insanSuresiOlculdu')
    if olculdu is True:
        durumlar.append('TEMIZ'); notlar.append('%s: %g/%g dk (kronometreli)' % (ad, toplam, tavan))
    elif olculdu is False:
        durumlar.append('UST_SINIR'); notlar.append('%s: %g/%g dk BEYAN (insanSuresiOlculdu=false)' % (ad, toplam, tavan))
    else:
        durumlar.append('KIRIK'); notlar.append('%s: insanSuresiOlculdu bayrağı YOK — ölçüm mü beyan mı bilinmiyor' % ad)

sonuc = 'KIRIK' if 'KIRIK' in durumlar else ('UST_SINIR' if 'UST_SINIR' in durumlar else 'TEMIZ')
print('%s|%s' % (sonuc, ' · '.join(notlar)))
PYEOF
)
P30_INSAN_DURUM=${P30_INSAN%%|*}
P30_INSAN_NOT=${P30_INSAN#*|}
P30_INSAN_BUGUN=$(date +%Y-%m-%d)
if [ "$P30_INSAN_DURUM" = "TEMIZ" ]; then
  p30_ok   "P-30: DEMO_INSAN_SURESI (BK-45)" "$P30_INSAN_NOT"
elif [ "$P30_INSAN_DURUM" = "UST_SINIR" ] && [ "$P30_INSAN_BUGUN" \> "$P30_INSAN_SON_TARIH" ]; then
  p30_kir  "P-30: DEMO_INSAN_SURESI (BK-45)" \
    "$P30_INSAN_NOT — son tarih $P30_INSAN_SON_TARIH geçti; kronometreli ölçüm ŞART"
elif [ "$P30_INSAN_DURUM" = "UST_SINIR" ]; then
  p30_uyar "P-30: DEMO_INSAN_SURESI (BK-45)" \
    "ÜST SINIR (beyan) — $P30_INSAN_NOT; $P30_INSAN_SON_TARIH'e dek geçerli, sonra KIRAR"
else
  p30_kir  "P-30: DEMO_INSAN_SURESI (BK-45)" "$P30_INSAN_NOT"
fi

# ── 3) DEMO_KISIT_AYRISMA = 0 (BK-47) ──────────────────────────────────────
# Giriş ekranına inen H kodları ile belgenin H satırları AYNI kümedir.
# ÖLÇÜM (TAZE, 2026-09-23): sözleşmede 8 kod, belgede 8 satır → fark 0.
if [ ! -f "$P30_SOZLESME" ] || [ ! -f "$P30_KAYNAK" ]; then
  p30_kir "P-30: DEMO_KISIT_AYRISMA (beklenen 0)" "sözleşme ya da kaynak belge YOK"
else
  P30_KOD=$(grep -oE 'new\("H-[1-8]"' "$P30_SOZLESME" | grep -oE 'H-[1-8]' | sort -u)
  P30_BELGE=$(grep -oE '^\| H-[1-8] ' "$P30_KAYNAK" | grep -oE 'H-[1-8]' | sort -u)
  if [ "$P30_KOD" = "$P30_BELGE" ] && [ -n "$P30_KOD" ]; then
    p30_ok "P-30: DEMO_KISIT_AYRISMA" "0 (giriş ekranı ile HARIC-LISTESI.md aynı H kümesi)"
  else
    p30_kir "P-30: DEMO_KISIT_AYRISMA (beklenen 0)" \
      "kod=[$(echo $P30_KOD | tr '\n' ' ')] belge=[$(echo $P30_BELGE | tr '\n' ' ')] — beyan ayrıştı (BK-47)"
  fi
fi

# <<< P-30

# >>> P-31 — MASKELİ DESTEK PAKETİ + HATA KODU (R-5 · T-4b · BK-17)
#     (cırcır; PLAN § 2 P-31 "Sayaçlar": TANILAMA_PAKETINDE_SIR = 0 ·
#      IC_KOD_SIZINTISI = 0 — ikincisi P-13'ten devralınır, P-31'in dokunduğu
#      üç dosyaya uygulanır.) İKİSİ DE MUTLAK SIFIR; tavan ARTIRILMAZ.
#
# ── SAYAÇ 1: TANILAMA_PAKETINDE_SIR = 0 ────────────────────────────────────
# NEDEN: destek paketi kiracının dışına (destek ekibine) çıkan TEK dosyadır.
#   Bir alan eklemek derleme hatası vermez, testten de kaçabilir; sayaç engeller.
# ÖLÇÜM KOMUTU (taze, 2026-09-23 — belgeden kopyalanmadı):
#   (a) DestekModulu.cs'te `// >>> PAKET-ICERIGI` … `// <<< PAKET-ICERIGI`
#       bölgelerinde (allow-list kayıtları + Derle metodu) kişisel veri/sır
#       alanı okuması ya da alan adı:
#         \.(Detay|IpAdresi|UserAgent|Ad|Soyad|Unvan|Tckn|Telefon|Eposta|Email|
#            Adres|Tutar|Bakiye|SifreHash|Gerekce|Aciklama)\b
#         \b(Unvan|Tckn|Telefon|Eposta|Email|Adres|Tutar|Bakiye|Parola|Sifre|
#            Token|Jeton|Gerekce|Detay|IpAdresi)\b  (tip adı ardından alan adı)
#         \bgerekce\b (kullanıcının serbest metni pakete giremez)
#   (b) fail-closed denetçi çağrısı YOKSA +1 (`TanilamaDenetcisi.Tara(`)
#   (c) kanal kapısının varsayılanı KAPALI değilse +1
#       (`GetValue(DestekAyarlari.KanalAcikAnahtari, false)`), depo
#       yapılandırması kanalı AÇIYORSA +1 (BK-17: Mehmet'in turu şart).
#   2026-09-23 taze ölçüm: (a) 0 · (b) 0 · (c) 0 → 0.
P31_MODUL="$API/Modules/Destek/DestekModulu.cs"
P31_SIR=0
P31_SIR_NOT=""
if [ ! -f "$P31_MODUL" ]; then
  P31_SIR=1; P31_SIR_NOT=" DestekModulu.cs YOK"
else
  P31_BOLGE=$(awk '/\/\/ >>> PAKET-ICERIGI/{b=1} /\/\/ <<< PAKET-ICERIGI/{b=0} b' "$P31_MODUL")
  P31_A1=$(printf '%s\n' "$P31_BOLGE" | grep -v '^[[:space:]]*//' | grep -cE '\.(Detay|IpAdresi|UserAgent|Ad|Soyad|Unvan|Tckn|Telefon|Eposta|Email|Adres|Tutar|Bakiye|SifreHash|Gerekce|Aciklama)\b')
  P31_A2=$(printf '%s\n' "$P31_BOLGE" | grep -v '^[[:space:]]*//' | grep -cE '(string|decimal|int|long|bool|DateTime)\??[[:space:]]+(Unvan|Tckn|Telefon|Eposta|Email|Adres|Tutar|Bakiye|Parola|Sifre|Token|Jeton|Gerekce|Detay|IpAdresi)\b')
  P31_A3=$(printf '%s\n' "$P31_BOLGE" | grep -v '^[[:space:]]*//' | grep -cE '\bgerekce\b')
  P31_B=$(grep -c 'TanilamaDenetcisi.Tara(' "$P31_MODUL" | tr -d ' ')
  P31_C1=$(grep -c 'GetValue(DestekAyarlari.KanalAcikAnahtari, false)' "$P31_MODUL" | tr -d ' ')
  P31_C2=$(grep -hiE '"PaketKanaliAcik"[[:space:]]*:[[:space:]]*true' "$API"/appsettings*.json 2>/dev/null | wc -l | tr -d ' ')
  P31_SIR=$((P31_A1 + P31_A2 + P31_A3))
  [ "$P31_B" -ge 1 ] || { P31_SIR=$((P31_SIR + 1)); P31_SIR_NOT="$P31_SIR_NOT denetçi çağrısı YOK"; }
  [ "$P31_C1" -ge 1 ] || { P31_SIR=$((P31_SIR + 1)); P31_SIR_NOT="$P31_SIR_NOT kanal varsayılanı kapalı DEĞİL"; }
  [ "$P31_C2" -eq 0 ] || { P31_SIR=$((P31_SIR + 1)); P31_SIR_NOT="$P31_SIR_NOT appsettings kanalı AÇIYOR (BK-17)"; }
  [ $((P31_A1 + P31_A2 + P31_A3)) -eq 0 ] || P31_SIR_NOT="$P31_SIR_NOT bölgede yasak alan: okuma=$P31_A1 ad=$P31_A2 gerekce=$P31_A3"
fi
if [ "$P31_SIR" -eq 0 ]; then
  printf "  ✓ %-46s %s (-eq 0: allow-list + denetçi + kanal kapalı)\n" "P-31: TANILAMA_PAKETINDE_SIR" "$P31_SIR"
else
  printf "  ✗ %-46s %s (-eq 0 olmalı —%s)\n" "P-31: TANILAMA_PAKETINDE_SIR" "$P31_SIR" "$P31_SIR_NOT"; HATA=1
fi

# ── SAYAÇ 2: IC_KOD_SIZINTISI = 0 (P-13'ten devralınır) ───────────────────
# KURAL (UX-S31): P-13'ün awk deseni AYNEN P-31'in üç dosyasına uygulanır;
#   ek desen: ekran hatasının HAM mesajı (`@istisna.Message`) basılmaz —
#   "The JSON value could not be converted to System.Int32 …" iç koddur.
# ÖLÇÜM KOMUTU: aşağıdaki awk + `grep -c '@istisna\.Message'`.
#   2026-09-23 taze ölçüm: P-31 ÖNCESİ 1 (KabukHatasi.razor `@istisna.Message`),
#   SONRASI 0 (Ayarlar 0 · HataBandi 0 · KabukHatasi 0).
P31_DOSYALAR=("$WEB/Pages/Ayarlar.razor" "$WEB/Bilesenler/HataBandi.razor" "$WEB/Bilesenler/KabukHatasi.razor")
P31_SIZINTI=0; P31_SIZINTI_LISTE=""; P31_TARANAN=0
for d in "${P31_DOSYALAR[@]}"; do
  [ -f "$d" ] || continue
  P31_TARANAN=$((P31_TARANAN + 1))
  bulgu=$(awk '
    /@\*/{yorum=1}
    { s=$0
      if(yorum){ if(s ~ /\*@/) yorum=0; next }
      if(s ~ /^[[:space:]]*(\/\/|@\*)/) next
      if(s ~ />[^<]*(PAKET|Paket) [0-9]/) { print NR; next }
      if(s ~ />[^<]*FAZ[_ ][A-Z]/) { print NR; next }
      if(s ~ /@[A-Za-z_.]*(HataKodu|IcKod)\b/) { print NR; next }
      if(s ~ />[^<]*[A-Z][A-Z0-9]{2,}_[A-Z0-9_]+/) { print NR; next }
      if(s ~ /@istisna\.Message/) { print NR; next }
    }' "$d" | wc -l | tr -d ' ')
  if [ "$bulgu" -gt 0 ]; then
    P31_SIZINTI=$((P31_SIZINTI + bulgu)); P31_SIZINTI_LISTE="$P31_SIZINTI_LISTE $(basename "$d"):$bulgu"
  fi
done
if [ "$P31_TARANAN" -ne 3 ]; then
  printf "  ✗ %-46s TARAMA YAPILMADI (%s/3 dosya) — sayac gecersiz\n" "P-31: IC_KOD_SIZINTISI" "$P31_TARANAN"; HATA=1
elif [ "$P31_SIZINTI" -eq 0 ]; then
  printf "  ✓ %-46s %s (-eq 0: %s dosya tarandı)\n" "P-31: IC_KOD_SIZINTISI" "$P31_SIZINTI" "$P31_TARANAN"
else
  printf "  ✗ %-46s %s (-eq 0 olmalı —%s)\n" "P-31: IC_KOD_SIZINTISI" "$P31_SIZINTI" "$P31_SIZINTI_LISTE"; HATA=1
fi
# <<< P-31

# >>> B-3 — PEŞİN MÜŞTERİ SİSTEM CARİSİ (karar D2 · K1–K5 · Mehmet VETO alanı)
#     Kural sunucudadır; bu blok onun SÖKÜLMESİNİ ve eşiğin istemciye göçünü sayar.
echo
echo "── B-3: Peşin Müşteri sistem carisi (fail-closed) ──"
# 1) KAPI_EKSIK = 0 — iki yasal kapının HER İKİ yolu (doğrulama + kümülatif
#    havuz) sistem carisini tanır. MasakKapisi: DogrulaKumulatif + GunlukIslemToplam;
#    TevsikKapisi: Dogrula + GunlukNakitToplam → her dosyada ≥ 2 çağrı.
B3_MASAK=$(grep -c 'SistemCarisi.MiAsync' "$API/Modules/Masak/MasakKapisi.cs" 2>/dev/null | tr -d ' ')
B3_TEVSIK=$(grep -c 'SistemCarisi.MiAsync' "$API/Modules/Tevsik/TevsikKapisi.cs" 2>/dev/null | tr -d ' ')
B3_KAPI_EKSIK=0
[ "${B3_MASAK:-0}" -ge 2 ] || B3_KAPI_EKSIK=$((B3_KAPI_EKSIK + 1))
[ "${B3_TEVSIK:-0}" -ge 2 ] || B3_KAPI_EKSIK=$((B3_KAPI_EKSIK + 1))
if [ "$B3_KAPI_EKSIK" -eq 0 ]; then
  printf "  ✓ %-46s 0 (masak=%s · tevsik=%s çağrı)\n" "B-3: KAPI_EKSIK" "$B3_MASAK" "$B3_TEVSIK"
else
  printf "  ✗ %-46s %s (masak=%s · tevsik=%s; her biri ≥ 2 olmalı)\n" "B-3: KAPI_EKSIK" "$B3_KAPI_EKSIK" "$B3_MASAK" "$B3_TEVSIK"; HATA=1
fi
# 2) KORUMA_EKSIK = 0 — silinemez/pasife alınamaz: SaveChanges'ın İKİ yolu da
#    korumayı çağırır, servis DeaktifEt kapısı ve karma fişin K3 bayrağı yerinde.
B3_KORUMA_EKSIK=0
grep -q 'SistemCarisiniKoru();' "$API/Data/KuyumcuDbContext.cs" || B3_KORUMA_EKSIK=$((B3_KORUMA_EKSIK + 1))
grep -q 'await SistemCarisiniKoruAsync(' "$API/Data/KuyumcuDbContext.cs" || B3_KORUMA_EKSIK=$((B3_KORUMA_EKSIK + 1))
grep -q 'throw new BadRequestException(SistemCarisi.SilinemezMesaji)' "$API/Modules/Cari/CariModulu.cs" || B3_KORUMA_EKSIK=$((B3_KORUMA_EKSIK + 1))
grep -q 'sistemCarisi: await Kuyumcu.Api.Modules.Cari.SistemCarisi.MiAsync' "$API/Modules/Satis/SatisModulu.cs" || B3_KORUMA_EKSIK=$((B3_KORUMA_EKSIK + 1))
if [ "$B3_KORUMA_EKSIK" -eq 0 ]; then
  printf "  ✓ %-46s 0\n" "B-3: KORUMA_EKSIK"
else
  printf "  ✗ %-46s %s (SaveChanges/servis/K3 kapısından biri sökülmüş)\n" "B-3: KORUMA_EKSIK" "$B3_KORUMA_EKSIK"; HATA=1
fi
# 3) ESIK_ISTEMCIDE — tavan 4 (cırcır). Yasal eşik SAYILARI istemciye gömülmez
#    (PLAN § 3 md.6). TABAN (2026-09-23, B-3 öncesi TAZE ölçüm): Kuyumcu.Web'de
#    4 satır — Servisler/TevsikOneri.cs:8 (yorum), Pages/Islemler.razor 2 yorum +
#    1 kod (TevsikHaddi geri düşüşü, B-3 öncesinden VAR, sahibi POS/tevsik
#    önizlemesi). B-3 bunlara DOKUNMADI; yeni satır ARTIŞ sayılır. Peşin Müşteri
#    sözlüğü (Kuyumcu.Core/I18n/PesinMusteriSozlugu.cs) da taranır.
B3_ESIK=$(grep -rnE '185[._ ]?000|30[._]000|30000' "$WEB" "Kuyumcu.Core/I18n/PesinMusteriSozlugu.cs" \
  --include='*.razor' --include='*.cs' 2>/dev/null | grep -v '/obj/' | grep -v '/bin/' | wc -l | tr -d ' ')
cirkir "B-3: ESIK_ISTEMCIDE" "$B3_ESIK" 4
# 4) ESIK_SABIT_SUNUCUDA = 0 — sistem carisi kuralı eşik sayısı TAŞIMAZ; sayı
#    yapılandırmadan (MasakEsikleri/TevsikEsikleri) okunur.
B3_SABIT=$(grep -cE '185[._ ]?000|30[._]000|30000' "$API/Modules/Cari/SistemCarisi.cs" 2>/dev/null | tr -d ' ')
if [ "${B3_SABIT:-0}" -eq 0 ]; then
  printf "  ✓ %-46s 0\n" "B-3: ESIK_SABIT_SUNUCUDA"
else
  printf "  ✗ %-46s %s (SistemCarisi.cs eşik sayısı taşıyor)\n" "B-3: ESIK_SABIT_SUNUCUDA" "$B3_SABIT"; HATA=1
fi
# <<< B-3

# >>> P-33 — KILAVUZ TEK KAYNAK + TEMİZ KURULUM + YEDEK TATBİKATI (B-11 · B-5) · CIRCIR
# Plan § 2 P-33 "Sayaçlar": KILAVUZ_EKSIK_EKRAN = 0 · KILAVUZ_URETILMIS ·
# ESKI_PORT_REFERANSI = 0 · YEDEK_TEK_YOL · YEDEK_TATBIKAT_BAYAT = 0.
# Hepsi hedef değerinde doğdu (taban 2026-09-23 TAZE ölçüldü, aşağıda); tavan
# ARTIRILMAZ — her biri sıfırdan büyük çıkarsa denetim KIRAR.
echo
echo "── P-33: kılavuz tek kaynak · adres/kısayol · yedek tatbikatı ──"
P33_MD="../docs/KULLANIM_KILAVUZU.md"
P33_SABLON="../docs/kilavuz-sablon.html"
P33_HARIC="../docs/HARIC-LISTESI.md"
P33_HTML="$WEB/wwwroot/kilavuz.html"
P33_ROTA="$WEB/Layout/RotaBasliklari.cs"
p33_sha() { shasum -a 256 "$1" 2>/dev/null | awk '{print $1}'; }

# 1) KILAVUZ_EKSIK_EKRAN = 0 — RotaBasliklari'ndaki her rota tek kaynakta
#    TAM satır "<!-- ekran: /rota -->" olarak işaretli olmalı.
#    ÖLÇÜM (TAZE, 2026-09-23): 47 rota · 47 işaret · eksik 0 (P-33 öncesi
#    eski kilavuz.html'de 23 bölüm vardı; 24 ekranın bölümü YOKTU).
#    grep -oE '^[[:space:]]*\["[^"]+"\]' RotaBasliklari.cs → rota listesi
P33_EKSIK=0; P33_EKSIK_AD=""; P33_ROTA_SAYI=0
if [ -f "$P33_MD" ] && [ -f "$P33_ROTA" ]; then
  while IFS= read -r _r; do
    [ -z "$_r" ] && continue
    P33_ROTA_SAYI=$((P33_ROTA_SAYI + 1))
    grep -qxF "<!-- ekran: $_r -->" "$P33_MD" || { P33_EKSIK=$((P33_EKSIK + 1)); P33_EKSIK_AD="$P33_EKSIK_AD $_r"; }
  done <<< "$(grep -oE '^[[:space:]]*\["[^"]+"\]' "$P33_ROTA" | sed -E 's/^[[:space:]]*\["//; s/"\]$//')"
else
  P33_EKSIK=1; P33_EKSIK_AD=" (kaynak ya da RotaBasliklari.cs YOK)"
fi
if [ "$P33_ROTA_SAYI" -eq 0 ]; then P33_EKSIK=1; P33_EKSIK_AD=" ÖLÇÜLEMEDİ(rota listesi boş)"; fi
if [ "$P33_EKSIK" -eq 0 ]; then
  printf "  ✓ %-46s 0 (%s rota, hepsi kılavuzda)\n" "P-33: KILAVUZ_EKSIK_EKRAN" "$P33_ROTA_SAYI"
else
  printf "  ✗ %-46s %s —%s\n" "P-33: KILAVUZ_EKSIK_EKRAN" "$P33_EKSIK" "$P33_EKSIK_AD"; HATA=1
fi

# 2) KILAVUZ_URETILMIS = 1 — kilavuz.html'in üretim damgası, üç kaynağın
#    (md · şablon · HARİÇ listesi) BUGÜNKÜ karmasını taşımalı. Kaynak değişip
#    dotnet/kilavuz-uret.sh koşulmadıysa ya da damga elle silindiyse 0 → KIRAR.
#    (HTML'in elle düzenlenmesini KilavuzUretimTestleri birebir karşılaştırmayla yakalar.)
P33_URETILMIS=0
if [ -f "$P33_HTML" ] && [ -f "$P33_MD" ] && [ -f "$P33_SABLON" ] && [ -f "$P33_HARIC" ]; then
  P33_DAMGA=$(grep -m1 'ÜRETİLDİ — ELLE DÜZENLEMEYİN' "$P33_HTML")
  if printf '%s' "$P33_DAMGA" | grep -qF "kaynak-sha256: $(p33_sha "$P33_MD")" \
     && printf '%s' "$P33_DAMGA" | grep -qF "haric-sha256: $(p33_sha "$P33_HARIC")" \
     && printf '%s' "$P33_DAMGA" | grep -qF "sablon-sha256: $(p33_sha "$P33_SABLON")"; then
    P33_URETILMIS=1
  fi
fi
if [ "$P33_URETILMIS" -eq 1 ]; then
  printf "  ✓ %-46s 1 (damga = md + şablon + HARİÇ karması)\n" "P-33: KILAVUZ_URETILMIS"
else
  printf "  ✗ %-46s 0 — kilavuz.html kaynaktan üretilmemiş/bayat: ./dotnet/kilavuz-uret.sh\n" "P-33: KILAVUZ_URETILMIS"; HATA=1
fi

# 3) ESKI_PORT_REFERANSI = 0 — (a) docs/*.md'de eski geliştirme adresleri
#    (localhost:3000/4000/5000) ve eski tek tık başlatıcı adları; (b) kullanıcı
#    belgesinde (kılavuz kaynağı + şablon + üretilen HTML) HERHANGİ bir sabit
#    port. Ters vekil arkasındaki iç bağlama noktası (127.0.0.1:5000, işletmeci
#    belgeleri) bu sayaca girmez: o bir yapılandırma değeridir, kullanıcı adresi değil.
#    ÖLÇÜM (TAZE, 2026-09-23, P-33 öncesi): (a) 7 satır — DOTNET.md:36,
#    MIMARI.md:53,300,336,338, KULLANIM_KILAVUZU.md:48,53,388 + DEPLOY.md:4;
#    (b) KULLANIM_KILAVUZU.md:50 localhost:3000. P-33 sonrası 0.
P33_PORT_A=$(grep -nE 'localhost:(3000|4000|5000)|start\.bat|stop\.bat' ../docs/*.md 2>/dev/null | wc -l | tr -d ' ')
P33_PORT_B=$(grep -nE 'localhost:[0-9]|127\.0\.0\.1:[0-9]|https?://[^[:space:]"<]*:[0-9]{2,5}' "$P33_MD" "$P33_SABLON" "$P33_HTML" 2>/dev/null | wc -l | tr -d ' ')
P33_PORT=$((P33_PORT_A + P33_PORT_B))
if [ "$P33_PORT" -eq 0 ]; then
  printf "  ✓ %-46s 0\n" "P-33: ESKI_PORT_REFERANSI"
else
  printf "  ✗ %-46s %s (docs eski adres/başlatıcı=%s · kullanıcı belgesinde sabit port=%s)\n" \
    "P-33: ESKI_PORT_REFERANSI" "$P33_PORT" "$P33_PORT_A" "$P33_PORT_B"; HATA=1
fi

# 4) YEDEK_TEK_YOL = 1 — tek yedek yolu dotnet/yedek-al-ve-dogrula.sh'tır:
#    (a) belgelerde (docs/*.md) ham dump KOMUTU yok (satır başında ya da bir
#        kabuk komutunun içinde mysqldump çağrısı; anlatım cümlesi sayılmaz),
#    (b) teslimat belgeleri (DEPLOY · YEDEK-GERI-YUKLEME · RUNBOOK-TESLIMAT)
#        tek yolu ADIYLA anar. İhlal sayısı 0 ise YEDEK_TEK_YOL = 1.
#    (Betik tarafı: T3-4 — dump çağrısı yalnız yedek-ortak.sh'ta.)
#    ÖLÇÜM (TAZE, 2026-09-23): (a) 0 · (b) RUNBOOK-TESLIMAT.md P-33 ile doğdu.
P33_TEK_IHLAL=$(grep -nE '^[[:space:]]*(sudo[[:space:]]+)?(docker[[:space:]]+exec[^#|]*[[:space:]])?mysqldump[[:space:]]' ../docs/*.md 2>/dev/null | wc -l | tr -d ' ')
for _b in DEPLOY.md YEDEK-GERI-YUKLEME.md RUNBOOK-TESLIMAT.md; do
  grep -q 'yedek-al-ve-dogrula.sh' "../docs/$_b" 2>/dev/null || P33_TEK_IHLAL=$((P33_TEK_IHLAL + 1))
done
if [ "$P33_TEK_IHLAL" -eq 0 ]; then
  printf "  ✓ %-46s 1 (belgelerde ikinci yedek komutu yok)\n" "P-33: YEDEK_TEK_YOL"
else
  printf "  ✗ %-46s 0 — %s ihlal (ham dump komutu ya da tek yolu anmayan teslimat belgesi)\n" "P-33: YEDEK_TEK_YOL" "$P33_TEK_IHLAL"; HATA=1
fi

# 5) YEDEK_TATBIKAT_BAYAT = 0 — son BAŞARILI geri yükleme tatbikatı 30 günden
#    eski olamaz (BK-30 · Köprü K-5). İki kaynak da ölçülür:
#    (a) KAYIT: docs/YEDEK-GERI-YUKLEME.md "SON_TATBIKAT: YYYY-MM-DD" satırı
#        (yoksa bayat sayılır — fail-closed);
#    (b) CANLI: kuyumcu-mysql kabı ayaktaysa kuyumcu_net.yedek_tatbikatlari'ndaki
#        son BASARILI satır (kap yoksa yalnız (a) ölçülür ve bu YAZILIR).
#    ÖLÇÜM (TAZE, 2026-09-23): (a) 0 gün · (b) 0 gün (t202609231915063497, 76/76).
P33_AZAMI_GUN=30
P33_BAYAT=0; P33_BAYAT_NOT=""
P33_KAYIT_TARIH=$(sed -n 's/^SON_TATBIKAT:[[:space:]]*\([0-9]\{4\}-[0-9]\{2\}-[0-9]\{2\}\).*/\1/p' ../docs/YEDEK-GERI-YUKLEME.md 2>/dev/null | head -1)
if [ -z "$P33_KAYIT_TARIH" ]; then
  P33_BAYAT=$((P33_BAYAT + 1)); P33_BAYAT_NOT="$P33_BAYAT_NOT kayıt=YOK"
else
  P33_KAYIT_GUN=$(perl -MTime::Local -e 'my ($y,$m,$d)=split /-/, $ARGV[0]; print int((time - timegm(0,0,0,$d,$m-1,$y))/86400)' "$P33_KAYIT_TARIH")
  if [ "${P33_KAYIT_GUN:-99999}" -gt "$P33_AZAMI_GUN" ]; then
    P33_BAYAT=$((P33_BAYAT + 1)); P33_BAYAT_NOT="$P33_BAYAT_NOT kayıt=${P33_KAYIT_GUN}gün"
  else
    P33_BAYAT_NOT="$P33_BAYAT_NOT kayıt=${P33_KAYIT_GUN}gün"
  fi
fi
if command -v docker >/dev/null 2>&1 && docker ps --format '{{.Names}}' 2>/dev/null | grep -qx "kuyumcu-mysql"; then
  P33_P=$(sed -n "s/^[[:space:]]*MYSQL_ROOT_PASSWORD:[[:space:]]*//p" ../docker-compose.yml | head -1 | tr -d "\r")
  P33_CANLI_GUN=$(docker exec kuyumcu-mysql mysql -uroot -p"$P33_P" -N kuyumcu_net -e \
    "SELECT COALESCE(TIMESTAMPDIFF(DAY, MAX(tarih), UTC_TIMESTAMP()), 99999) FROM yedek_tatbikatlari WHERE sonuc='BASARILI';" 2>/dev/null | tr -d ' \r')
  P33_P=""
  if [ -z "$P33_CANLI_GUN" ]; then
    P33_BAYAT=$((P33_BAYAT + 1)); P33_BAYAT_NOT="$P33_BAYAT_NOT canlı=ÖLÇÜLEMEDİ"
  elif [ "$P33_CANLI_GUN" -gt "$P33_AZAMI_GUN" ]; then
    P33_BAYAT=$((P33_BAYAT + 1)); P33_BAYAT_NOT="$P33_BAYAT_NOT canlı=${P33_CANLI_GUN}gün"
  else
    P33_BAYAT_NOT="$P33_BAYAT_NOT canlı=${P33_CANLI_GUN}gün"
  fi
else
  P33_BAYAT_NOT="$P33_BAYAT_NOT canlı=kap-yok(yalnız kayıt ölçüldü)"
fi
if [ "$P33_BAYAT" -eq 0 ]; then
  printf "  ✓ %-46s 0 (azami %s gün;%s)\n" "P-33: YEDEK_TATBIKAT_BAYAT" "$P33_AZAMI_GUN" "$P33_BAYAT_NOT"
else
  printf "  ✗ %-46s %s (azami %s gün;%s) — ./dotnet/yedek-al-ve-dogrula.sh tatbikat\n" \
    "P-33: YEDEK_TATBIKAT_BAYAT" "$P33_BAYAT" "$P33_AZAMI_GUN" "$P33_BAYAT_NOT"; HATA=1
fi
# <<< P-33

# >>> S2-3 — FİŞ ÜST SATIRI: PEŞİN MÜŞTERİ BANDI + BAKİYE ROZETİ (plan § S-2 madde 2.4 + 2.6)
#     Bandı sunucunun ALAN İŞARETLİ reddi açar (400 + alanlar.cariId); eşik sayısı
#     istemcide yok. Bu blok işaretin SÖKÜLMESİNİ, fiş bileşeninin istek atmasını
#     ve sözlüğe sayı sızmasını sayar.
echo
echo "── S2-3: Peşin Müşteri bandı + bakiye rozeti ──"
# 1) ALAN_ISARETI_EKSIK = 0 — Peşin eşik reddinin üç yolu da cariId ile işaretli
#    (MasakKapisi: kimlik + tevsik · TevsikKapisi: fiş/nakit) ve ara katman işareti okur.
S23_MASAK=$(grep -c 'AlanIle("cariId")' "$API/Modules/Masak/MasakKapisi.cs" 2>/dev/null | tr -d ' ')
S23_TEVSIK=$(grep -c 'AlanIle("cariId")' "$API/Modules/Tevsik/TevsikKapisi.cs" 2>/dev/null | tr -d ' ')
S23_ALAN_EKSIK=0
[ "${S23_MASAK:-0}" -ge 2 ] || S23_ALAN_EKSIK=$((S23_ALAN_EKSIK + 1))
[ "${S23_TEVSIK:-0}" -ge 1 ] || S23_ALAN_EKSIK=$((S23_ALAN_EKSIK + 1))
grep -q 'var alan = AlanIsareti.Alan(ex);' "$API/Common/ApiException.cs" || S23_ALAN_EKSIK=$((S23_ALAN_EKSIK + 1))
if [ "$S23_ALAN_EKSIK" -eq 0 ]; then
  printf "  ✓ %-46s 0 (masak=%s · tevsik=%s)\n" "S2-3: ALAN_ISARETI_EKSIK" "$S23_MASAK" "$S23_TEVSIK"
else
  printf "  ✗ %-46s %s (masak=%s ≥2 · tevsik=%s ≥1 · ara katman)\n" "S2-3: ALAN_ISARETI_EKSIK" "$S23_ALAN_EKSIK" "$S23_MASAK" "$S23_TEVSIK"; HATA=1
fi
# 2) FIS_BILESENI_ISTEK = 0 — tek POST yolu (plan § 3 md.5): yeni fiş parçaları
#    ApiIstemci/HttpClient bilmez. Ölçüt KOD desenidir (enjeksiyon/istek çağrısı);
#    yorumdaki "ApiIstemci bilmez" cümlesi sayılmaz.
S23_ISTEK=$(cat "$WEB/Bilesenler/Fis/FisMusteriSatiri.razor" "$WEB/Bilesenler/Fis/FisBakiyeRozeti.cs" 2>/dev/null \
  | grep -cE '@inject[[:space:]]+(ApiIstemci|HttpClient)|\[Inject\]|[A-Za-z]\.(Get|Post|Patch|Put|Delete|Send)[A-Za-z]*Async|new HttpClient' | tr -d ' ')
if [ "${S23_ISTEK:-0}" -eq 0 ]; then
  printf "  ✓ %-46s 0\n" "S2-3: FIS_BILESENI_ISTEK"
else
  printf "  ✗ %-46s %s (fiş bileşeni istek atmaz)\n" "S2-3: FIS_BILESENI_ISTEK" "$S23_ISTEK"; HATA=1
fi
# 3) SOZLUK_SAYI = 0 — bant/rozet metinlerinde rakam yok (eşik sayısı istemciye gömülmez).
S23_SAYI=$(grep -E '^[[:space:]]*[A-Za-z]+:[[:space:]]*"|^[[:space:]]*\+[[:space:]]*"' "Kuyumcu.Core/I18n/Sozlukler.FisMusteri.cs" 2>/dev/null \
  | grep -cE '"[^"]*[0-9][^"]*"' | tr -d ' ')
if [ "${S23_SAYI:-0}" -eq 0 ]; then
  printf "  ✓ %-46s 0\n" "S2-3: SOZLUK_SAYI"
else
  printf "  ✗ %-46s %s (Sozlukler.FisMusteri.cs metninde rakam)\n" "S2-3: SOZLUK_SAYI" "$S23_SAYI"; HATA=1
fi
# <<< S2-3

# >>> OLCEK-1 — MOD ÖLÇÜ DEĞİŞTİRMEZ (2026-09-25; kurucu: "emanet al butonuna bi bak boyut değişiyor")
# MOD_OLCU_BILDIRIMI = 0: tüm wwwroot/css/*.css'te html[data-mod="basit"|"profesyonel"]
# seçicisinde ölçü belirteci (--kontrol-yuksekligi, --dokunma-hedefi, --dugme-yuksekligi*,
# --bosluk-hucre, --satir-yuksekligi, --yazi-*) ya da doğrudan ölçü (height, min/max-height,
# padding*, font-size, font, font-weight, line-height, border-radius) YAZILMAZ. İstisna: (a) adıyla
# beyaz liste (tuval-kabuk.css PRO kenar menüsü yüzü — KATMAN/T-K2), (b) olcek.css'te
# `html[data-mod] body …` nötrleyicisi olan bildirim. Aynı kural: OlcekTekTestleri.OL2.
olcek1_say() {
python3 - "$1" <<'PYEOF'
import re, sys, os, glob
d = sys.argv[1]
MOD = re.compile(r'html\[data-mod\s*=\s*["\']?(basit|profesyonel)["\']?\s*\]')
BEL = ("--kontrol-yuksekligi", "--dokunma-hedefi", "--dugme-yuksekligi", "--bosluk-hucre", "--satir-yuksekligi", "--yazi-")
# v5 (27.09): font-weight (kalınlık) da ölçüdür — spesifikasyon "kalınlık" farkını sayar.
OZ = re.compile(r'^(height|min-height|max-height|padding(-(top|right|bottom|left|block|inline)(-(start|end))?)?|font-size|font|font-weight|line-height|border-radius|border-(top|bottom|start|end)-(left|right|start|end)-radius)$')
BEYAZ = []   # tema temizliği 17.10: tek istisna (tuval-kabuk.css PRO kenar menüsü) kabukla kalktı
bs = lambda s: re.sub(r'\s+', ' ', s).strip()
def bildirimler(css):
    css = re.sub(r'/\*.*?\*/', '', css, flags=re.S); y = []; buf = []; out = []
    def bos():
        p = ''.join(buf).strip(); buf.clear()
        if ':' not in p or p.index(':') == 0: return
        o, v = p.split(':', 1)
        out.append((bs(' '.join(x for x in y if x.startswith('@'))), ' '.join(x for x in y if not x.startswith('@')), o.strip(), v.strip()))
    for ch in css:
        if ch == '{': y.append(''.join(buf).strip().replace('\n', ' ')); buf.clear()
        elif ch == '}':
            bos()
            if y: y.pop()
        elif ch == ';': bos()
        else: buf.append(ch)
    return out
par = lambda s: [bs(x) for x in s.split(',') if bs(x)]
olcek = bildirimler(open(os.path.join(d, 'olcek.css')).read())
ihlal = []
for f in sorted(glob.glob(os.path.join(d, '*.css'))):
    ad = os.path.basename(f)
    for at, sec, oz, v in bildirimler(open(f).read()):
        if not (MOD.search(sec) or 'data-mod="basit"' in sec or 'data-mod="profesyonel"' in sec): continue
        bel = oz.startswith(BEL); dog = bool(OZ.match(oz))
        if not bel and not dog: continue
        if not bel and any(ad == w and all(p.startswith(o) for p in par(sec)) for w, o in BEYAZ): continue
        if not bel and ad != 'olcek.css' and all(any(o[2] == oz and o[0] == at and MOD.sub('html[data-mod]', p).replace('html[data-mod] ', 'html[data-mod] body ') in par(o[1]) for o in olcek) for p in par(sec)): continue
        ihlal.append(f"{ad}: {at} {bs(sec)} {{ {oz}: {v} }}")
for x in ihlal: print(x, file=sys.stderr)
print(len(ihlal))
PYEOF
}
OLC_SAYI=$(olcek1_say "$WEB/wwwroot/css")
if [ "${OLC_SAYI:-x}" = 0 ]; then printf "  ✓ %-46s %s\n" "OLCEK-1: MOD_OLCU_BILDIRIMI" "0"
else printf "  ✗ %-46s %s\n" "OLCEK-1: MOD_OLCU_BILDIRIMI (beklenen 0)" "${OLC_SAYI:-hesaplanamadı}"; HATA=1; fi
# v6 (28.09, doğrulayıcı DÜŞÜK): mod yüzü SINIFLARI (.tuval-pro, .tuval-tezgah, .*sade) da iki
# modu ayrı çizer; olcek.css onları özgüllükle ezer — ezemeyeceği tek şey !important'tır.
# YUZ_ONEMLI_OLCU = 0: olcek.css dışındaki hiçbir css'te yüz sınıfı ya da html[data-mod=…]
# seçicisinde !important ölçü bildirimi yok. Aynı kural: OlcekTekTestleri.OL16.
olcek1_yuz_say() {
python3 - "$1" <<'PYEOF'
import re, sys, os, glob
d = sys.argv[1]
YUZ = re.compile(r'\.(tuval-pro|tuval-tezgah|[\w-]*sade)(?![\w-])|html\[data-mod\s*=')
BEL = ("--kontrol-yuksekligi", "--dokunma-hedefi", "--dugme-yuksekligi", "--bosluk-hucre", "--satir-yuksekligi", "--yazi-")
OZ = re.compile(r'^(height|min-height|max-height|padding(-(top|right|bottom|left|block|inline)(-(start|end))?)?|font-size|font|font-weight|line-height|border-radius|border-(top|bottom|start|end)-(left|right|start|end)-radius)$')
n = 0
for f in sorted(glob.glob(os.path.join(d, '*.css'))):
    if os.path.basename(f) == 'olcek.css': continue
    css = re.sub(r'/\*.*?\*/', '', open(f).read(), flags=re.S)
    for m in re.finditer(r'([^{};]+)\{([^{}]*)\}', css):
        if not YUZ.search(m.group(1)): continue
        for dek in m.group(2).split(';'):
            if ':' not in dek: continue
            o, v = dek.split(':', 1); o = o.strip()
            if '!important' in v.lower() and (OZ.match(o) or o.startswith(BEL)):
                n += 1; print(f"{os.path.basename(f)}: {' '.join(m.group(1).split())} {{ {o}: {v.strip()} }}", file=sys.stderr)
print(n)
PYEOF
}
OLC_YUZ=$(olcek1_yuz_say "$WEB/wwwroot/css")
if [ "${OLC_YUZ:-x}" = 0 ]; then printf "  ✓ %-46s %s\n" "OLCEK-1: YUZ_ONEMLI_OLCU" "0"
else printf "  ✗ %-46s %s\n" "OLCEK-1: YUZ_ONEMLI_OLCU (beklenen 0)" "${OLC_YUZ:-hesaplanamadı}"; HATA=1; fi
# v7 (28.09 gece, çapraz hakem KRİTİK — doğrulayıcı M3): `.tuval-pro #islem-kaydet { height: 32px }`
# gibi mod yüzü SINIFIYLA (!important'sız) yazılan kontrol ölçüsü ne MOD_OLCU_BILDIRIMI'ne ne
# YUZ_ONEMLI_OLCU'ya takılıyordu. YUZ_OLCU_ENVANTER_DISI = 0: olcek.css dışındaki css'lerde yüz sınıfı
# (.tuval-pro/.tuval-tezgah/.*sade) taşıyan seçici parçası KONTROL öznesine (#kimlik, button/input/
# select/textarea/summary, .btn*, .girdi*, .secici-tetik, .sekme, .ekran-karo, .fis-salt-kutu) ölçü
# yazıyorsa satır Kuyumcu.Tests/OlcekYuzEnvanteri.txt'te BİREBİR bulunur. Aynı kural: OlcekTekTestleri.OL26.
olcek1_env_say() {
python3 - "$1" "$2" <<'PYEOF'
import re, sys, os, glob
d, envy = sys.argv[1], sys.argv[2]
OZ = re.compile(r'^(height|min-height|max-height|padding(-(top|right|bottom|left|block|inline)(-(start|end))?)?|font-size|font|font-weight|line-height|border-radius|border-(top|bottom|start|end)-(left|right|start|end)-radius)$')
YUZ = re.compile(r'\.(tuval-pro|tuval-tezgah|[\w-]*sade)(?![\w-])')
KON = re.compile(r'#[\w-]+|^(button|input|select|textarea|summary)(?![\w-])|\.(btn|girdi)[\w-]*|\.(secici-tetik|sekme|ekran-karo|fis-salt-kutu)(?![\w-])')
bs = lambda s: re.sub(r'\s+', ' ', s).strip()
def bildirimler(css):
    css = re.sub(r'/\*.*?\*/', '', css, flags=re.S); y = []; buf = []; out = []
    def bos():
        p = ''.join(buf).strip(); buf.clear()
        if ':' not in p or p.index(':') == 0: return
        o, v = p.split(':', 1)
        out.append((' '.join(x for x in y if x.startswith('@')), ' '.join(x for x in y if not x.startswith('@')), o.strip(), v.strip()))
    for ch in css:
        if ch == '{': y.append(''.join(buf).strip().replace('\n', ' ')); buf.clear()
        elif ch == '}':
            bos()
            if y: y.pop()
        elif ch == ';': bos()
        else: buf.append(ch)
    return out
def ozne(p):
    while True:
        t = re.sub(r'\([^()]*\)', '', p)
        if t == p: break
        p = t
    p = re.sub(r'::?[\w-]+', '', p)
    k = [x for x in re.split(r'[\s>+~]+', p.strip()) if x]
    return k[-1] if k else ''
try:
    env = set(x.strip() for x in open(envy) if x.strip() and not x.strip().startswith('#'))
except OSError:
    print('envanter-yok'); sys.exit(0)
disi = []
for f in sorted(glob.glob(os.path.join(d, '*.css'))):
    ad = os.path.basename(f)
    if ad == 'olcek.css': continue
    for at, sec, o, v in bildirimler(open(f).read()):
        if not OZ.match(o): continue
        for p in [bs(x) for x in sec.split(',') if bs(x)]:
            if YUZ.search(p) and KON.search(ozne(p)):
                k = f"{ad} | {bs(at)} | {p} | {o} | {bs(v)}"
                if k not in env and k not in disi: disi.append(k)
for x in disi: print(x, file=sys.stderr)
print(len(disi) if env else 'envanter-bos')
PYEOF
}
OLC_ENV=$(olcek1_env_say "$WEB/wwwroot/css" "Kuyumcu.Tests/OlcekYuzEnvanteri.txt")
if [ "${OLC_ENV:-x}" = 0 ]; then printf "  ✓ %-46s %s\n" "OLCEK-1: YUZ_OLCU_ENVANTER_DISI" "0"
else printf "  ✗ %-46s %s\n" "OLCEK-1: YUZ_OLCU_ENVANTER_DISI (beklenen 0)" "${OLC_ENV:-hesaplanamadı}"; HATA=1; fi
# <<< OLCEK-1

# >>> DUGME-2 — DÜĞME YAZISI DİKEY ORTADA + DÜĞME ZEMİNDEN AYIRT EDİLİR (2026-09-29; kurucu 25.09
# Faturalar: "butonlar zeminle aynı renkte"). Sayaçlar (hepsi 0 beklenir):
#  DUGME2_BAGLANTI_SIRASI : index.html css/dugme.css'i olcek.css'ten SONRA, ortak.css'ten hemen ÖNCE yüklemiyor
#  DUGME2_SABIT_RENK      : dugme.css'te (yorumlar hariç) #hex / rgb() / hsl() / color-mix() / url( — renk yalnız var()
#  DUGME2_KURAL_EKSIK     : dugme.css'te yerleşik select (appearance:none), basılı düğme (aria-pressed=true,
#                           lacivert --secili-dolgu) ya da segment KAP kenarı (--sekme-kenar) kuralı yok; yapışık (sticky)
#                           hücre var (tur 3: veriyi örtüyordu); /vitrin eylem-altı ızgarası 1280'i kapsamıyor;
#                           /urunler PRO STOK düğmesi eylem satırına inmiyor; /ekranlar favori yıldızı dinlenirken gizli
# Aynı kurallar: DugmeIkiTestleri.D2_01 / D2_02 / D2_04–D2_06. Görsel kanıt: dugme-olc.mjs (rapor).
dugme2_say() {
python3 - "$1" "$2" <<'PYEOF'
import re, sys
css_yol, index_yol = sys.argv[1], sys.argv[2]
try:
    css = open(css_yol, encoding='utf-8').read()
except OSError:
    print("99 99 99"); sys.exit(0)
kod = re.sub(r'/\*.*?\*/', '', css, flags=re.S)
html = re.sub(r'<!--.*?-->', '', open(index_yol, encoding='utf-8').read(), flags=re.S)
l = re.findall(r'<link rel="stylesheet" href="([^"]+)"', html)
sira = 0
if 'css/dugme.css' not in l or 'css/olcek.css' not in l: sira = 1
elif not (l.index('css/olcek.css') < l.index('css/dugme.css') and l[-1] == 'css/ortak.css' and l.index('css/dugme.css') == len(l) - 2): sira = 1
renk = len(re.findall(r'#[0-9a-fA-F]{3,8}\b|\b(?:rgba?|hsla?|color-mix)\(|url\(', kod))
eksik = 0
if not re.search(r'select:not\(\[multiple\]\)[^{]*\{[^}]*\bappearance:\s*none', kod): eksik += 1
# KESIF-P7 (30.09, TOPLU DUGME-2 [YÜKSEK] tuval gerilemesi): basılı/seçili dolgu tuvalin lacivert anahtar
# dolgusudur (--secili-dolgu; ana eylem mavisi DEĞİL); kaplamalı segmentte seçili olmayan öğe kendi çerçevesini
# taşımaz (1px solid transparent), ≥ 3:1 sınırı KAP kenarı (--sekme-kenar) verir. Aynı kural: KesifP7Testleri.
if not re.search(r'\[aria-pressed="true"\][^{]*\{[^}]*background:\s*var\(--secili-dolgu\)', kod): eksik += 1
if not re.search(r':is\(\.sekmeler-serit, \.musteri-segment\)\s*\{[^}]*border:\s*1px solid var\(--sekme-kenar\)', kod) \
   or not re.search(r'\.sekmeler-sekme[^{]*\{[^}]*border:\s*1px solid transparent', kod): eksik += 1
# Tur 3 (tasarım hakemi 29.09 16:15): yapışık (sticky) hücre veriyi örtüyordu → dugme.css'te
# yapışık konum YOK; /vitrin eylemleri 769–(≥1280) aralığında satırın altında (ızgara), /urunler
# PRO STOK düğmesi 769–1099 ızgarasında eylem satırına iner. /ekranlar yıldızı dinlenirken görünür.
if re.search(r"position:\s*sticky", kod): eksik += 1
m = re.search(r"@media \(min-width: 769px\) and \(max-width: (\d+)px\)\s*\{\s*html\[data-mod\] \.ana-icerik #vitrin-ayarlar\s*\{[^}]*display:\s*grid", kod)
if not m or int(m.group(1)) < 1280 or int(m.group(1)) >= 1440: eksik += 1
if not re.search(r'td:has\(> \[id\^="u-stok-adet-"\]\)\s*\{[^}]*grid-column:\s*1 / 2', kod): eksik += 1
if not re.search(r':is\(#fl-tablo, #km-tablo, [^{]*\)\) > tbody > tr > td:last-child:not\(\[colspan\]\)\s*\{[^}]*grid-column:\s*1 / -1', kod): eksik += 1
if not re.search(r"\.ekran-yildiz\s*\{[^}]*opacity:\s*1\b", kod): eksik += 1
print(sira, renk, eksik)
PYEOF
}
read D2_SIRA D2_RENK D2_EKSIK <<< "$(dugme2_say "$WEB/wwwroot/css/dugme.css" "$WEB/wwwroot/index.html")"
for d2 in "DUGME2_BAGLANTI_SIRASI:${D2_SIRA:-x}" "DUGME2_SABIT_RENK:${D2_RENK:-x}" "DUGME2_KURAL_EKSIK:${D2_EKSIK:-x}"; do
  d2ad=${d2%%:*}; d2d=${d2#*:}
  if [ "$d2d" = 0 ]; then printf "  ✓ %-46s %s\n" "DUGME-2: $d2ad" "0"
  else printf "  ✗ %-46s %s\n" "DUGME-2: $d2ad (beklenen 0)" "$d2d"; HATA=1; fi
done
# <<< DUGME-2

# >>> YASAL-1 — tarihli yasal parametreler (şartname § 4.1 / § 6 K-8)
# YASAL_PARAM_TARIHSIZ = 0: mevzuat eşiği ENJEKTE EDİLEN yapılandırma nesnesinden
#   (TevsikEsikleri/MasakEsikleri, küçük harf `esikler.`) ya da tarihsiz kapı
#   özelliğinden (`.Esikler.Had`) okunmaz; işlem gününün tablosundan okunur.
#   Kural sınıfları (TevsikKurali, MasakKuralMotoru) tarihi çözülmüş nesneyle
#   çalıştığı için hariçtir; DI'sız test yedeği satır sonunda işaretlidir.
#   ÖLÇÜM: grep -rnE "esikler\.(Had|KimlikEsigi|…)|\.Esikler\.Had" Modules
YASAL_TARIHSIZ=$(grep -rnE '\besikler\.(Had|KimlikEsigi|AmacBeyaniEsigi|BeyanFormuEsigi|EkBelgeEsigi|SupheliBildirimIsGunu|SaklamaYili)\b|\.Esikler\.Had\b' "$API/Modules" --include='*.cs' 2>/dev/null \
  | grep -v -E 'Modules/Tevsik/TevsikKurali\.cs|Modules/Masak/MasakKuralMotoru\.cs' \
  | grep -v "YASAL-1: DI'sız yedek" | grep -vE '^[^:]+:[0-9]+:[[:space:]]*//' | wc -l | tr -d ' ')
# YASAL_PARAM_YAZMA = 0: append-only tablo — Remove/ExecuteUpdate/ExecuteDelete YOK.
#   ÖLÇÜM: grep -rnE "YasalParametreler\.(Remove|ExecuteUpdate|ExecuteDelete)" Kuyumcu.Api (Migrations hariç)
YASAL_YAZMA=$(grep -rnE 'YasalParametreler(\.Where\([^)]*\))?\.(Remove|RemoveRange|ExecuteUpdate|ExecuteDelete)|Set<YasalParametre>\(\)\.(Remove|Execute)' "$API" --include='*.cs' 2>/dev/null \
  | grep -v '/Migrations/' | grep -v '/obj/' | wc -l | tr -d ' ')
# YASAL_PARAM_OKUMA_DISI = 0: kiracı-üstü tablo YALNIZ YasalParametreServisi'nden okunur
#   (+ DbContext tanımı, migration'lar, sistem sağlık ucu).
YASAL_OKUMA=$(grep -rln 'YasalParametreler\b' "$API" --include='*.cs' 2>/dev/null \
  | grep -v -E '/Migrations/|/obj/|Data/KuyumcuDbContext\.cs|Common/YasalParametreServisi\.cs|Modules/Sistem/SistemModulu\.cs' | wc -l | tr -d ' ')
for y in "YASAL_PARAM_TARIHSIZ:$YASAL_TARIHSIZ" "YASAL_PARAM_YAZMA:$YASAL_YAZMA" "YASAL_PARAM_OKUMA_DISI:$YASAL_OKUMA"; do
  yad=${y%%:*}; yd=${y#*:}
  if [ "$yd" = 0 ]; then printf "  ✓ %-46s %s\n" "YASAL-1: $yad" "0"
  else printf "  ✗ %-46s %s (beklenen 0)\n" "YASAL-1: $yad" "$yd"; HATA=1; fi
done
# <<< YASAL-1

# >>> YASAL-3 — MASAK brüt ölçü · saklama bekletmesi · yaptırım listesi (şartname § 4)
# YASAL3_OLCU_IKI_SORGU = 0 eksik: GunlukIslemToplam masak_olcu_tl'yi İKİ AYRIK sorguyla okur
#   (A5 kararı — COALESCE/CASE bileşimi yok). ÖLÇÜM: MasakKapisi.cs'te iki Where deseni + COALESCE 0.
Y3_KAPI="$API/Modules/Masak/MasakKapisi.cs"
Y3_OLCU=0
grep -q 'Where(h => h.MasakOlcuTl != null)' "$Y3_KAPI" || Y3_OLCU=$((Y3_OLCU+1))
grep -q 'Where(h => h.MasakOlcuTl == null)' "$Y3_KAPI" || Y3_OLCU=$((Y3_OLCU+1))
Y3_OLCU=$((Y3_OLCU + $(grep -vE '^[[:space:]]*//' "$Y3_KAPI" | grep -ciE 'coalesce|\?\? *h\.TlTutari|MasakOlcuTl *\?\?' || true)))
# YASAL3_SILME = 0: yaptırım listesi / eşleşme / saklama bekletmesi SATIRI SİLİNMEZ.
Y3_SIL=$(grep -rnE '(YaptirimKayitlari|YaptirimEslesmeleri|SaklamaBekletmeleri)(\.Where\([^)]*\))?\.(Remove|RemoveRange|ExecuteUpdate|ExecuteDelete)' "$API" --include='*.cs' 2>/dev/null \
  | grep -v '/Migrations/' | grep -v '/obj/' | wc -l | tr -d ' ')
for y in "YASAL3_OLCU_IKI_SORGU_EKSIK:$Y3_OLCU" "YASAL3_SATIR_SILME:$Y3_SIL"; do
  yad=${y%%:*}; yd=${y#*:}
  if [ "$yd" = 0 ]; then printf "  ✓ %-46s %s\n" "YASAL-3: $yad" "0"
  else printf "  ✗ %-46s %s (beklenen 0)\n" "YASAL-3: $yad" "$yd"; HATA=1; fi
done
# <<< YASAL-3

# >>> PAKET CARI-BAKIYE — TEK YAZICI + CANLI BAKİYE AYNASI (has + TL) · 30.09 ekip oyu
# (1) CARI_BAKIYE_DOGRUDAN_YAZIM = 0: cari_bakiyeler.tl_bakiye / has_gram_bakiye kolonuna
#     atama, CariBakiye nesnesi kurma, ExecuteUpdate ya da ham UPDATE/INSERT YALNIZ
#     Common/CariBakiyeServisi.cs (CariBakiyeYazici) içinde olur. Başka yerde yazılırsa bir
#     sonraki kural değişikliğinde orası unutulur (30.09: beş modülde beş ayrı yazıcı vardı).
#     ÖLÇÜM: kod_say (yorum/Migrations/obj hariç) · BEKLENEN İHLAL ÇIKTISI:
#       ✗ CARI_BAKIYE_DOGRUDAN_YAZIM (tek yazıcı dışı)   1 (beklenen 0)
#         → Modules/X/Y.cs:NN:        mevcut.TlBakiye += tlDelta;
denetle "CARI_BAKIYE_DOGRUDAN_YAZIM (tek yazıcı dışı)" \
  '(^|[^A-Za-z])(TlBakiye|HasGramBakiye)[[:space:]]*[+-]?=([^=>]|$)|new[[:space:]]+(Data\.)?CariBakiye([^A-Za-z]|$)|CariBakiyeler[^;]*Execute(Update|Delete)|(UPDATE|INSERT[[:space:]]+INTO)[[:space:]]+`?cari_bakiyeler' \
  0 'Common/CariBakiyeServisi\.cs'

# (2) CANLI: kuyumcu_net'in TÜM kiracılarında kayıtlı bakiye ↔ kural aynası uyuşmazlığı.
#     has: |kayıtlı − Σ kural| > 0,0001 · TL: |kayıtlı − Σ kural| > 0,01 (TL yalnız ÖLÇÜLEBİLİR
#     carilerde: veresiye payı NULL (Faz 0 öncesi) satırı ya da konsinye sahipliği olan cari
#     "ölçülemez" sayılır ve ayrıca raporlanır — uydurulmaz). Kural C# ile birebir:
#     Common/CariBakiyeServisi.cs CariBakiyeKurali (HasEtkisi · GrupTlEtkisi · DegisimVeresiyeFarki).
#     BİLİNEN TOHUM HATASI (beyaz liste DEĞİL — sayaç onları da ölçer ve basar): ByGatech demo
#     kiracısının 3 carisi (09.09 21:09 Faz 0 öncesi kodla yazılmış veri; kök neden
#     ham/CARI-BAKIYE-UYUSMAZLIK-2026-09-30.md; kurucu kararıyla silinecek). Bu üçü DIŞINDA bir
#     uyuşmazlık = ✗. Üçünden biri artık uyuşuyorsa listeden çıkarılması uyarılır.
#     + 4. satır (TL aynasının İLK ölçümünde bulundu, 30.09): Demo Kuyumculuk kiracısının
#     "Kurban Musteri" carisi (01M240M0MV…) — 09.09 21:19 K1 saldırı gösterimi; satışı BAŞKA
#     kiracı (ByGatech) yazmış, bakiye 77.777 TL. Kiracılar arası tek satır (SQL: 1). Aynı tohum
#     penceresi, aynı karar sahibi (kurucu). Ayna kiracıya kilitli ölçer; satır sayılmaz.
#     KARAR BAĞI (tur 2, 01.10 — hasım bulgusu ORTA): ekip oyu (3) yalnız ÜÇ cariyi adlandırır.
#     4. satır o oyun kapsamında DEĞİLDİR; ayrı ekip kararıyla (Tamer Bey + Taşkın + Mustafa +
#     Mehmet, 01.10, PAKET-CARI-BAKIYE-uygulama-raporu.md § 13 "Karar K-CB-4") ayrı listede tutulur:
#     kiracılar arası yazım kalıntısıdır (güvenlik delili — Mehmet: silinmeden önce kurucu görür),
#     kod hatası değildir; silme/yeniden hesap kurucu kararıdır. Çıktı iki listeyi AYRI sayar.
CB_BILINEN_TOHUM_OY3="01M2401B30M3SVRK1FDWNEXNRC 01M2401B38BQD9E2V5WW5K21N0 01M2401B3SASCYK7YQNCBRX26G"
CB_BILINEN_TOHUM_KCB4="01M240M0MVM6EZG460S80RPV2F"
CB_BILINEN_TOHUM="$CB_BILINEN_TOHUM_OY3 $CB_BILINEN_TOHUM_KCB4"
CB_OLCULDU=0
CB_SATIRLAR=""
CB_OLCULEMEZ=""
if command -v docker >/dev/null 2>&1 && docker ps --format '{{.Names}}' 2>/dev/null | grep -qx "kuyumcu-mysql"; then
  CB_PAROLA=$(sed -n "s/^[[:space:]]*MYSQL_ROOT_PASSWORD:[[:space:]]*//p" ../docker-compose.yml 2>/dev/null | head -1 | tr -d "\r")
  if [ -n "${CB_PAROLA:-}" ]; then
    CB_SQL="
      SELECT b.tenant_id, b.cari_id,
             ROUND(b.has_gram_bakiye - COALESCE(a.hs,0), 6) AS hf,
             CASE WHEN k.cari_id IS NOT NULL OR COALESCE(a.bilinmez,0) > 0 THEN 'OLCULEMEZ'
                  ELSE ROUND(b.tl_bakiye - COALESCE(a.tl,0) - COALESCE(d.fark,0), 2) END AS tf
        FROM cari_bakiyeler b
        LEFT JOIN (SELECT tenant_id, cari_id,
               SUM(CASE WHEN tur IN ('VERESIYE','TAHSILAT','CARI_DUZELTME','ACILIS') THEN -has_grami
                        WHEN tur IN ('EMANET_GIRIS','EMANET_CIKIS') AND maden = 'ALTIN' THEN -has_grami
                        ELSE 0 END) AS hs,
               SUM(CASE WHEN tur IN ('VERESIYE','ACILIS','CARI_DUZELTME','EVRAK_RUCU') THEN tl_tutari
                        WHEN tur = 'TAHSILAT' THEN CASE WHEN (CASE WHEN storno_kaynak_id IS NULL THEN tl_tutari ELSE -tl_tutari END) < 0 THEN tl_tutari ELSE 0 END
                        WHEN tur IN ('AVANS_ALINDI','AVANS_IADE') THEN -tl_tutari
                        ELSE SIGN(tl_tutari) * COALESCE(veresiye_tutar,0) END) AS tl,
               SUM(CASE WHEN veresiye_tutar IS NULL AND tl_tutari <> 0
                         AND tur NOT IN ('VERESIYE','ACILIS','CARI_DUZELTME','EVRAK_RUCU','TAHSILAT','AVANS_ALINDI','AVANS_IADE')
                        THEN 1 ELSE 0 END) AS bilinmez
             FROM hareketler WHERE cari_id IS NOT NULL GROUP BY tenant_id, cari_id) a
          ON a.tenant_id = b.tenant_id AND a.cari_id = b.cari_id
        LEFT JOIN (SELECT DISTINCT tenant_id, konsinye_cari_id AS cari_id FROM urunler
                    WHERE konsinye_cari_id IS NOT NULL) k
          ON k.tenant_id = b.tenant_id AND k.cari_id = b.cari_id
        LEFT JOIN (SELECT x.tenant_id, x.cari_id, SUM(LEAST(x.f, 0)) AS fark FROM (
                     SELECT h.tenant_id, h.cari_id, h.fis_id, SUM(h.tl_tutari) AS f FROM hareketler h
                      WHERE h.tur IN ('SATIS','SATIS_IADE') AND EXISTS (
                            SELECT 1 FROM hareketler s
                             WHERE s.tenant_id = h.tenant_id AND s.cari_id = h.cari_id AND s.fis_id = h.fis_id
                               AND s.tur = 'SATIS' AND s.storno_kaynak_id IS NULL AND s.odeme_turu = 'VERESIYE'
                               AND s.veresiye_tutar = 0 AND s.tl_tutari > 0
                               AND EXISTS (SELECT 1 FROM hareketler i WHERE i.tenant_id = s.tenant_id
                                            AND i.fis_id = s.fis_id AND i.cari_id = s.cari_id AND i.tur = 'SATIS_IADE'))
                      GROUP BY h.tenant_id, h.cari_id, h.fis_id) x
                   GROUP BY x.tenant_id, x.cari_id) d
          ON d.tenant_id = b.tenant_id AND d.cari_id = b.cari_id"
    CB_HAM=$(docker exec kuyumcu-mysql mysql -uroot -p"$CB_PAROLA" -N kuyumcu_net -e "$CB_SQL" 2>/dev/null)
    CB_RC=$?
    if [ "$CB_RC" -eq 0 ]; then
      CB_OLCULDU=1
      CB_SATIRLAR=$(printf '%s\n' "$CB_HAM" | awk -F'\t' 'NF>=4 {
          h=$3+0; t=$4; tu=0;
          if (t != "OLCULEMEZ") { tv=t+0; if (tv > 0.01 || tv < -0.01) tu=1 }
          if (h > 0.0001 || h < -0.0001 || tu) print $2 "\t" $3 "\t" $4 }')
      CB_OLCULEMEZ=$(printf '%s\n' "$CB_HAM" | awk -F'\t' '$4=="OLCULEMEZ"' | grep -c . || true)
    fi
  fi
fi
if [ "$CB_OLCULDU" -eq 1 ]; then
  CB_TOPLAM=$(printf '%s' "$CB_SATIRLAR" | grep -c . || true)
  CB_YENI=0; CB_BILINEN=0; CB_BILINEN_KCB4=0; CB_YENI_LISTE=""
  if [ -n "$CB_SATIRLAR" ]; then
    while IFS="$(printf '\t')" read -r cb_cari cb_hf cb_tf; do
      [ -z "$cb_cari" ] && continue
      case " $CB_BILINEN_TOHUM " in
        *" $cb_cari "*) CB_BILINEN=$((CB_BILINEN + 1))
          case " $CB_BILINEN_TOHUM_KCB4 " in *" $cb_cari "*) CB_BILINEN_KCB4=$((CB_BILINEN_KCB4 + 1)) ;; esac ;;
        *) CB_YENI=$((CB_YENI + 1)); CB_YENI_LISTE="$CB_YENI_LISTE
      → $cb_cari has farkı $cb_hf · TL farkı $cb_tf" ;;
      esac
    done <<CBEOF
$CB_SATIRLAR
CBEOF
  fi
  if [ "$CB_YENI" = 0 ]; then
    printf "  ✓ %-46s %s\n" "CARI_BAKIYE_AYNA_UYUSMAZ (canlı, has+TL)" \
      "ölçülen $CB_TOPLAM · bilinen tohum hatası $CB_BILINEN · yeni 0"
  else
    printf "  ✗ %-46s %s\n" "CARI_BAKIYE_AYNA_UYUSMAZ (canlı, has+TL)" \
      "ölçülen $CB_TOPLAM · bilinen tohum hatası $CB_BILINEN · YENİ $CB_YENI (beklenen 0)"
    printf '%s\n' "$CB_YENI_LISTE" | sed '/^$/d'
    HATA=1
  fi
  if [ "$CB_BILINEN" -gt 0 ]; then
    printf "  ⚠ %-46s %s\n" "CARI_BAKIYE bilinen tohum hatası (NOT)" \
      "$((CB_BILINEN - CB_BILINEN_KCB4)) cari ekip oyu (3) · ByGatech demo 09.09 Faz 0 öncesi tohum — kurucu kararıyla silinecek"
    [ "$CB_BILINEN_KCB4" -gt 0 ] && printf "  ⚠ %-46s %s\n" "CARI_BAKIYE bilinen tohum hatası (NOT, K-CB-4)" \
      "$CB_BILINEN_KCB4 cari · Demo Kuyumculuk 'Kurban Musteri' 09.09 K1 kiracılar arası yazım kalıntısı — ayrı ekip kararı 01.10, silme kurucuda"
  fi
  for cb_b in $CB_BILINEN_TOHUM; do
    printf '%s\n' "$CB_SATIRLAR" | grep -q "^$cb_b" \
      || printf "  ⚠ %-46s %s\n" "CARI_BAKIYE bilinen listede ama uyuşuyor" "$cb_b — listeden çıkarın"
  done 2>/dev/null
  printf "  · %-46s %s\n" "CARI_BAKIYE TL aynası ölçülemez cari (bilgi)" \
    "${CB_OLCULEMEZ:-0} (Faz 0 öncesi veresiye payı yok ya da konsinye sahibi — uydurulmaz)"
else
  printf "  ✗ %-46s %s\n" "CARI_BAKIYE_AYNA_UYUSMAZ (canlı)" "ÖLÇÜLEMEDİ (kuyumcu-mysql yok ya da sorgu hatası) — sessiz yeşil DEĞİL"
  HATA=1
fi
# <<< PAKET CARI-BAKIYE

# >>> PAKET TOPLU-1 — TARIH-TEK (BELGE-KALAN 01.10, YÜKSEK; beyaz liste YOK)
# Kural: ekrandaki HER tarih/saat Servisler/Bicim.cs'ten geçer (Bicim.IsletmeZamani → Europe/Istanbul).
# Razor ve sayfa/bileşen/yerleşim kodunda (Pages, Bilesenler, Layout · .razor + .cs) şunlar 0:
#   · .ToLocalTime(   (tarayıcının saat dilimi — işletme günü değil)
#   · .ToString("…dd/yyyy/HH/MMMM/dddd…")   (ham tarih kalıbı; makine biçimi dâhil → Bicim.Makine)
#   · .ToString("g"|"d"|"t"|"f"…)            (tek harfli tarih kalıbı)
#   · $"{x:dd.MM…}" / {x:yyyy-MM-dd} / {x:HH:mm} (ara-değer kalıbı)
# Servisler/ (Bicim, YasalBicim … biçim katmanı) sayılmaz. Aynı kural: Toplu1Testleri.TARIH_TEK_*.
tarih_tek_say() {
python3 - "$1" <<'PYEOF2'
import re, sys, os, glob
kok = sys.argv[1]
DES = [re.compile(r'\.ToLocalTime\s*\('),
       re.compile(r'\.ToString\(\s*"[^"]*(dd|yyyy|yy|HH|hh|MMMM|dddd)[^"]*"'),
       re.compile(r'\.ToString\(\s*"[gdtfGDTF]"'),
       re.compile(r'\{[^{}"]*:(dd|yyyy|HH|hh)[^{}]*\}'),
       # TOPLU-1 tur 2 (tasarım hakemi KRİTİK): tek kalıp dd.MM.yyyy — sıfırsız gün ("d.MM") ve
       # kültürün kısa kalıbı ({x:d}/{x:g}) iki biçim üretir; Bicim'e serbest kalıpla da geçemez.
       re.compile(r'(IsletmeBicim|GunBicim)\([^;]*"d(\.|\'\.\')M'),
       re.compile(r'\{[^{}"]*:[dgG]\}')]
n = 0
for alt in ('Pages', 'Bilesenler', 'Layout'):
    for f in sorted(glob.glob(os.path.join(kok, alt, '**', '*.razor'), recursive=True) + glob.glob(os.path.join(kok, alt, '**', '*.cs'), recursive=True)):
        if '/obj/' in f or '/bin/' in f: continue
        for i, s in enumerate(open(f, encoding='utf-8'), 1):
            t = s.lstrip()
            if t.startswith(('//', '@*', '*', '///')): continue
            if any(d.search(s) for d in DES):
                n += 1; print(f"    {os.path.relpath(f, kok)}:{i}: {t.strip()[:110]}", file=sys.stderr)
print(n)
PYEOF2
}
TARIH_TEK_LISTE=$(tarih_tek_say "$WEB" 2>&1 >/dev/null)
TARIH_TEK=$(tarih_tek_say "$WEB" 2>/dev/null)
if [ "${TARIH_TEK:-x}" = 0 ]; then printf "  ✓ %-46s %s\n" "TOPLU-1: TARIH_TEK (ham tarih biçimi/ToLocalTime)" "0"
else printf "  ✗ %-46s %s\n" "TOPLU-1: TARIH_TEK (beklenen 0)" "${TARIH_TEK:-hesaplanamadı}"; printf '%s\n' "$TARIH_TEK_LISTE" | head -20; HATA=1; fi
# <<< PAKET TOPLU-1

# >>> PAKET GRID-1 — HAM_TABLO_ORTAK_GRID_DISI (envanter GIRDI-KURALLARI-ENVANTER-2026-10-08 § F3 · § G)
# Ortak ızgara (Kuyumcu.Web/Bilesenler/Grid/Izgara.razor) DIŞINDA yazılmış ham <table> sayısı.
# CIRCIR: yalnız azalır. Taban 96 (08.10 ölçümü, 52 dosya; yazdırma belgeleri DAHİL — envanter
# sayısıyla birebir). GRID-1 örnek ekranı /cariler listesini ızgaraya taşıdı → 95. Göç paketleri
# (GOC-A..D) ekran taşıdıkça tavanı AŞAĞI çeker; yukarı çekmek YASAK (gevşetme = ihlal).
# Yazdırma belgesi tabloları (class="belge-tablo") kâğıt düzenidir, ızgaraya taşınmaz — bilgi
# olarak ayrıca basılır (istisna sayısı da artamaz: tavan 4).
# Fail-closed: sayı ölçülemezse ✗ (sessiz yeşil yok); ortak ızgarada tam 1 <table> olmalı.
# PAKET GOC-A (08.10): para ekranlarının 25 tablosu ortak ızgaraya taşındı → 95 → 70 (cırcır).
# PAKET GOC-B (08.10): kimlik ve uyum ekranlarının 12 tablosu ortak ızgaraya taşındı → 70 → 58 (cırcır).
#   (NakitSiniriAsimlari dilekçe tablosu yazdırma belgesidir; belge tavanı gevşetilmedi, ham sayılmaya devam eder.)
# PAKET GOC-C (08.10): ürün ve fiş ekranlarının 23 tablosu ortak ızgaraya taşındı → 58 → 35 (cırcır).
#   (Gerekçeli istisna, ham sayılmaya devam eder: satış tuvalinin 4 tablosu — FisKalemTablosu ×2, FisVerdimAldim,
#    FisSonSatislar: satır içi düzenleme/satır altı kırılım ızgarada yok — ve Vitrin #vitrin-ayarlar: başlıktaki
#    "değiştirilebilir" rozeti ızgara başlığında taşınamıyor. Ortak ızgaraya başlık/satır-altı şablonu gelince taşınır.)
# PAKET GOC-D (09.10): yönetim ve rapor ekranlarının 22 tablosu ortak ızgaraya taşındı → 35 → 13 (cırcır).
#   (Gerekçeli istisna, ham sayılmaya devam eder: Raporlar #yas-tablo — sütunlara hizalı alt toplam satırları
#    (tfoot, colspan döviz özeti) + başlıkta ⓘ ipucu ızgarada yok; İşlem defteri #id-yazdir-tablo — ekranda
#    görünmeyen kâğıt belgesi, belge tavanı (4) gevşetilmedi.)
# PAKET GOC-C-2 (09.10): satış tuvalinin 4 tablosu (FisKalemTablosu ×2, FisVerdimAldim, FisSonSatislar) ve Vitrin
#   #vitrin-ayarlar ortak ızgaraya taşındı (GOC-C istisnaları kapandı: hücre şablonu + ızgara altı panel + rozet satırı) → 13 → 8.
GRID1_TAVAN=8
GRID1_BELGE_TAVAN=4
GRID1_SAY=$(python3 - <<'PYEOF_GRID1'
import re, pathlib
kok = pathlib.Path("Kuyumcu.Web")
grid = kok / "Bilesenler" / "Grid"
n = belge = ortak = 0
for f in sorted(kok.rglob("*.razor")):
    if "/obj/" in f.as_posix() or "/bin/" in f.as_posix():
        continue
    m = f.read_text(encoding="utf-8")
    m = re.sub(r"@\*.*?\*@", "", m, flags=re.S)   # GRID-1 tur 2: Razor yorumları (@* … *@) sayılmaz
    for t in re.finditer(r"<table\b[^>]*>", m):
        if grid in f.parents:
            ortak += 1
            continue
        n += 1
        if "belge-tablo" in t.group(0):
            belge += 1
print(f"{n} {belge} {ortak}")
PYEOF_GRID1
)
GRID1_HAM="${GRID1_SAY%% *}"; GRID1_KALAN="${GRID1_SAY#* }"; GRID1_BELGE="${GRID1_KALAN%% *}"; GRID1_ORTAK="${GRID1_KALAN#* }"
case "$GRID1_HAM$GRID1_BELGE$GRID1_ORTAK" in
  ''|*[!0-9]*) printf "  ✗ %-46s %s\n" "GRID-1: HAM_TABLO_ORTAK_GRID_DISI" "ÖLÇÜLEMEDİ — sessiz yeşil DEĞİL"; HATA=1 ;;
  *)
    if [ "$GRID1_HAM" -le "$GRID1_TAVAN" ]; then
      printf "  ✓ %-46s %s (tavan %s, yalnız azalır; yazdırma belgesi %s)\n" "GRID-1: HAM_TABLO_ORTAK_GRID_DISI" "$GRID1_HAM" "$GRID1_TAVAN" "$GRID1_BELGE"
    else
      printf "  ✗ %-46s %s (tavan %s — ARTIŞ: yeni liste ortak ızgarayla yazılır)\n" "GRID-1: HAM_TABLO_ORTAK_GRID_DISI" "$GRID1_HAM" "$GRID1_TAVAN"; HATA=1
    fi
    if [ "$GRID1_BELGE" -gt "$GRID1_BELGE_TAVAN" ]; then
      printf "  ✗ %-46s %s (tavan %s)\n" "GRID-1: yazdırma belgesi tablosu (istisna)" "$GRID1_BELGE" "$GRID1_BELGE_TAVAN"; HATA=1
    fi
    if [ "$GRID1_ORTAK" != 1 ]; then
      printf "  ✗ %-46s %s (beklenen 1)\n" "GRID-1: ortak ızgaradaki <table>" "$GRID1_ORTAK"; HATA=1
    fi ;;
esac
# <<< PAKET GRID-1

# >>> PAKET GIRDI-AILESI-2 — F3 CIRCIR SAYACLARI (envanter GIRDI-KURALLARI-ENVANTER-2026-10-08 § F3)
# Kurucu 08.10: "her veri türüne TEK anlamsal bileşen; TC kontrolü bir yerde var diğerinde yok olmasın".
#   SAYISAL_OLU_SINIR       = 0 (fail-closed): SayisalAlan EnAz/EnCok'u UYGULAMIYORSA her EnAz=/EnCok= çağrısı sayılır.
#   IBAN_REGEX_DTO          = 0 (fail-closed): IBAN [RegularExpression] ile değil [Iban] (Core MOD 97-10) ile doğrulanır.
#   TEST_GECERSIZ_FIKSTUR   = 0 (fail-closed): testlerde K1 IBAN / K5 TCKN / K2 telefon kuralına uymayan OLUMLU fikstür
#                             ("TR"+rastgele üretici dâhil). Bilinçli olumsuz: satırda "// olumsuz-fikstür" ya da dosya
#                             başında "olumsuz-fikstür: dosya"; ±4 satırda Throws/400/BadRequest geçen değer sayılmaz (serbest "hata" sözcüğü muaf DEĞİL).
#   Cırcır (YALNIZ AZALIR, tavan 08.10 ölçümü — göç paketleri GOC-A..D indirir):
#   HAM_TARIH_GIRDISI ≤ 7 · GIRDI_TUR_GENEL_BILESEN ≤ 15 · DTO_DECIMAL_RANGESIZ ≤ 26 ·
#   EKRAN_SUNUCU_UZUNLUK_FARKI ≤ 168 (VEKİL ölçü: ekran ile DTO sınırı kodda eşlenemediği için
#   AlanSinirlari sabitinden okunmayan sayısal EnFazla/maxlength sayılır; hedef 0 = her sınır tek sabitten).
girdi2_say() {
python3 - "$1" <<'PYEOF_G2'
import re, sys, os, glob
kok = '.'; hangi = sys.argv[1]
WEB = os.path.join(kok, 'Kuyumcu.Web'); API = os.path.join(kok, 'Kuyumcu.Api'); TST = os.path.join(kok, 'Kuyumcu.Tests')
def dosyalar(d, uz):
    for f in sorted(glob.glob(os.path.join(d, '**', '*' + uz), recursive=True)):
        if '/obj/' in f or '/bin/' in f: continue
        yield f
def satirlar(f):
    yorum = False
    for i, s in enumerate(open(f, encoding='utf-8'), 1):
        yield i, s
def razorlar():
    for alt in ('Pages', 'Bilesenler', 'Layout'):
        yield from dosyalar(os.path.join(WEB, alt), '.razor')
def yaz(f, i, s): print(f"    {os.path.relpath(f, kok)}:{i}: {s.strip()[:110]}", file=sys.stderr)
n = 0
if hangi == 'SAYISAL_OLU_SINIR':
    sa = open(os.path.join(WEB, 'Bilesenler/Form/SayisalAlan.cs'), encoding='utf-8').read()
    uygulaniyor = re.search(r'SayiKurali\.Aralik\(\s*d\s*,\s*EnAz\s*,\s*EnCok\s*\)', sa) and re.search(r'KuralDenetle\(d\)\s*is\s*\{', sa)
    if not uygulaniyor:
        for f in razorlar():
            for i, s in satirlar(f):
                if re.search(r'\bEn(Az|Cok)="', s): n += 1; yaz(f, i, s)
elif hangi == 'HAM_TARIH_GIRDISI':
    for f in razorlar():
        if f.endswith('Bilesenler/Form/Tarih.razor'): continue
        for i, s in satirlar(f):
            if re.search(r'type="date"', s) and not s.lstrip().startswith(('@*', '//')): n += 1; yaz(f, i, s)
elif hangi == 'IBAN_REGEX_DTO':
    for f in dosyalar(API, '.cs'):
        sat = open(f, encoding='utf-8').read().split('\n')
        sinif = None
        for i, s in enumerate(sat, 1):
            if re.search(r'RegularExpression\(@?"[^"]*TR\\d', s): n += 1; yaz(f, i, s)
            # PAKET GOC-A (GIRDI-AILESI-2 kapanış DÜŞÜK devri): Dto sınıfında 'Iban' adlı string
            # özellik [Iban] (Core MOD 97-10) taşımıyorsa sayılır — yalnız uzunluk/regex yetmez.
            m = re.search(r'\b(class|record)\s+(\w+)', s)
            if m: sinif = m.group(2)
            if sinif and sinif.endswith('Dto') and re.search(r'^\s*(\[[^\]]*\]\s*)*public\s+string\??\s+\w*Iban\s*\{', s):
                onceki = '\n'.join(sat[max(0, i - 4):i])
                if not re.search(r'\[(\w+,\s*)*Iban\b', onceki): n += 1; yaz(f, i, s)
elif hangi == 'GIRDI_TUR_GENEL_BILESEN':
    ANLAM = re.compile(r'(?i)(tckn|vkn|kimlik-?no|vergi-?no|iban|telefon|-tel\b|eposta|e-posta|tarih|tutar|gram|milyem)')
    for f in razorlar():
        if '/Bilesenler/Form/' in f: continue
        icerik = open(f, encoding='utf-8').read().split('\n')
        for i, s in enumerate(icerik, 1):
            m = re.search(r'<(Metin|Sayi)\s[^>]*?ElemanId="([^"]+)"', s)
            if m and ANLAM.search(m.group(2)): n += 1; yaz(f, i, s); continue
            m = re.search(r'<input\b[^>]*\bid="([^"]+)"', s)
            if m and ANLAM.search(m.group(1)) and not re.search(r'type="(checkbox|radio|hidden|file|search)"', s): n += 1; yaz(f, i, s)
elif hangi == 'EKRAN_UZUNLUK_SABITSIZ':
    for f in razorlar():
        if '/Bilesenler/Form/' in f: continue
        for i, s in satirlar(f):
            for m in re.finditer(r'\b(EnFazla|maxlength)="(\d+)"', s): n += 1; yaz(f, i, s)
elif hangi == 'DTO_DECIMAL_RANGESIZ':
    KURAL = re.compile(r'\[(Range|Tutar|Gram|Has|Milyem|Adet|Yuzde|Kur|GunSayisi)\b|\b(Range|Tutar|Gram|Has|Milyem|Adet|Yuzde|Kur|GunSayisi)(\(|\])')
    for f in dosyalar(API, '.cs'):
        satir = open(f, encoding='utf-8').read().split('\n')
        sinif = None
        for i, s in enumerate(satir, 1):
            m = re.search(r'\b(class|record)\s+(\w+)', s)
            if m: sinif = m.group(2)
            if not sinif or not sinif.endswith('Dto'): continue
            if re.search(r'^\s*(\[[^\]]*\]\s*)*public\s+decimal\??\s+\w+\s*\{\s*get', s):
                onceki = '\n'.join(satir[max(0, i - 4):i])
                if not KURAL.search(onceki): n += 1; yaz(f, i, s)
elif hangi == 'TEST_GECERSIZ_FIKSTUR':
    def mod97(x):
        x = x[4:] + x[:4]; r = 0
        for c in x:
            if c.isdigit(): r = (r * 10 + int(c)) % 97
            elif 'A' <= c <= 'Z': r = (r * 100 + ord(c) - 55) % 97
            else: return -1
        return r
    def tckn(no):
        if len(no) != 11 or not no.isdigit() or no[0] == '0': return False
        h = [int(c) for c in no]
        d10 = ((sum(h[0:9:2]) * 7 - sum(h[1:8:2])) % 10 + 10) % 10
        return d10 == h[9] and sum(h[:10]) % 10 == h[10]
    MOBIL = {501, 505, 506, 507, 510, 516, 524, 551, 552, 553, 554, 555, 559, 592} | set(range(530, 550))
    def tel(no):
        u = no[1:] if no.startswith('0') else no
        if len(u) != 10: return False
        k = int(u[:3])
        return u[0] in '234' or (u[0] == '5' and k in MOBIL) or (u[0] == '8' and k in (800, 850))
    MUAF = {'TcknDogrulamaTestleri.cs', 'GiderPusulasiTestleri.cs', 'P16SatisKalemiTestleri.cs', 'S21FisBilesenTestleri.cs',
            'CariEkranTestleri.cs', 'Q103IsletmeKimlikTestleri.cs', 'SATIS3SinirTestleri.cs', 'P31DestekPaketiTestleri.cs',
            'P31TanilamaTestleri.cs', 'UX4dEkranTestleri.cs', 'CumaMMusterilerTestleri.cs', 'TotpTestleri.cs'}
    # Muafiyet DAR (doğrulayıcı tur 1): yalnız açık işaret yorumu ya da reddi İDDİA eden satır (Throws / 400 / BadRequest).
    # Çevredeki serbest 'hata/geçersiz' sözcüğü geçersiz fikstürü GİZLEYEMEZ.
    OLUMSUZ = re.compile(r'(Throws|\b400\b|BadRequest|olumsuz-fikstür)')
    TCKN_BAGLAM = re.compile(r'(?i)(tckn|belgeno|tespit\(|sifrele\(|hashle\(|tckimlik)')
    TEL_BAGLAM = re.compile(r'(?i)(telefon|\btel\b|gsm)')
    for f in dosyalar(TST, '.cs'):
        ad = os.path.basename(f)
        if ad in MUAF: continue
        sat = open(f, encoding='utf-8').read().split('\n')
        if any('olumsuz-fikstür: dosya' in x for x in sat[:30]): continue
        for i, s in enumerate(sat, 1):
            if 'olumsuz-fikstür' in s or re.search(r'Assert\.False\(', s): continue   # aynı satırda reddi iddia eden olumsuz test
            cevre = '\n'.join(sat[max(0, i - 5):i + 4])
            for m in re.finditer(r'"(TR[0-9A-Z]{24})"', s):
                if mod97(m.group(1)) != 1 and not OLUMSUZ.search(cevre): n += 1; yaz(f, i, s)
            if re.search(r'"TR"\s*\+\s*(Random|new string|string\.Concat)', s): n += 1; yaz(f, i, s)
            # Araya ekleme kalıbı ($"TR{...}") da üreticidir — doğrulayıcı tur 1 kör noktası (PosYardim.cs:115).
            elif re.search(r'\$@?"TR\{', s): n += 1; yaz(f, i, s)
            if TCKN_BAGLAM.search(s):
                for m in re.finditer(r'"([1-9]\d{10})"', s):
                    if not tckn(m.group(1)) and not OLUMSUZ.search(cevre): n += 1; yaz(f, i, s)
            if TEL_BAGLAM.search(s):
                for m in re.finditer(r'"(0\d{10})"', s):
                    if not tel(m.group(1)) and not OLUMSUZ.search(cevre): n += 1; yaz(f, i, s)
print(n)
PYEOF_G2
}
for g2 in "SAYISAL_OLU_SINIR:0:sifir" "IBAN_REGEX_DTO:0:sifir" "TEST_GECERSIZ_FIKSTUR:0:sifir" \
          "HAM_TARIH_GIRDISI:0:tavan" "GIRDI_TUR_GENEL_BILESEN:0:tavan" "DTO_DECIMAL_RANGESIZ:10:tavan" \
          "EKRAN_UZUNLUK_SABITSIZ:8:tavan"; do   # PAKET GOC-A (08.10): 7→6 · 15→11 · 26→20 · 168→129 · PAKET GOC-B (08.10): 11→9 · 20→17 · 129→86 (kimlik/uyum ekranları) · PAKET GOC-C (08.10): 6→5 · 9→2 · 17→10 · 86→30 (ürün ve fiş ekranları) · PAKET GOC-D (09.10): 5→0 · 2→0 · 30→8 (yönetim ve rapor ekranları; kalan 8 YasalParamFormu — paket dışı)
  g2_ad="${g2%%:*}"; g2_kalan="${g2#*:}"; g2_tavan="${g2_kalan%%:*}"; g2_tur="${g2_kalan#*:}"
  g2_gorunen="$g2_ad"; [ "$g2_ad" = "EKRAN_UZUNLUK_SABITSIZ" ] && g2_gorunen="EKRAN_SUNUCU_UZUNLUK_FARKI (vekil)"
  g2_liste=$(girdi2_say "$g2_ad" 2>&1 >/dev/null)
  g2_say=$(girdi2_say "$g2_ad" 2>/dev/null)
  case "$g2_say" in ''|*[!0-9]*) printf "  ✗ %-46s %s\n" "GIRDI-2: $g2_gorunen" "ÖLÇÜLEMEDİ — sessiz yeşil DEĞİL"; HATA=1; continue;; esac
  if [ "$g2_say" -le "$g2_tavan" ]; then
    if [ "$g2_tur" = sifir ]; then printf "  ✓ %-46s %s\n" "GIRDI-2: $g2_gorunen" "0 (fail-closed)"
    else printf "  ✓ %-46s %s (tavan %s, yalnız azalır)\n" "GIRDI-2: $g2_gorunen" "$g2_say" "$g2_tavan"; fi
  else
    printf "  ✗ %-46s %s (tavan %s)\n" "GIRDI-2: $g2_gorunen" "$g2_say" "$g2_tavan"; printf '%s\n' "$g2_liste" | head -20; HATA=1
  fi
done
# <<< PAKET GIRDI-AILESI-2

# >>> PAKET DIL-EKRAN — EN_MODDA_TURKCE_BASLIK (08.10; kurucu: "ingilizceye cevirince ekran isimleri turkce kaliyor")
# Razor ISARETLEMESINDE (@code disi, yorumlar atilir) sozluk disi Turkce harfli CIPLAK metin dugumu ya da
# title/aria-label/placeholder/alt literali sayisi. Bu metinler EN modda da Turkce gorunur. Hedef 0 (fail-closed).
# Baslangic degeri 1 idi (Urunler.razor "Kayıtlı adet"); tur 2'de sayac birim literali ("… has"/"… adet") ve
# ham sunucu maden adi (X.MadenAdi) siniflarini da kapsar. Simdi 0 — yalniz AZALIR, tavan 0.
en_turkce_baslik_say() {
python3 - "$1" <<'PYEOF_DE'
import re, sys, glob, os
kok = sys.argv[1]
n = 0
for f in sorted(glob.glob(kok + '/Kuyumcu.Web/**/*.razor', recursive=True)):
    if '/obj/' in f or '/bin/' in f: continue
    s = open(f, encoding='utf-8').read()
    s = re.sub(r'@\*.*?\*@', lambda m: '\n' * m.group(0).count('\n'), s, flags=re.S)
    s = re.sub(r'<!--.*?-->', lambda m: '\n' * m.group(0).count('\n'), s, flags=re.S)
    i = s.find('@code')
    isaret = s if i < 0 else s[:i]
    for ln, l in enumerate(isaret.split('\n'), 1):
        for m in re.finditer(r'>([^<>@{}]*[çğıİöşüÇĞÖŞÜ][^<>@{}]*)<', l):
            n += 1; print('%s:%d: %s' % (os.path.relpath(f, kok), ln, m.group(1).strip()[:70]), file=sys.stderr)
        for m in re.finditer(r'\b(title|aria-label|placeholder|alt)="([^"@]*[çğıİöşüÇĞÖŞÜ][^"@]*)"', l):
            n += 1; print('%s:%d: %s=%s' % (os.path.relpath(f, kok), ln, m.group(1), m.group(2)[:60]), file=sys.stderr)
# BIRIM/AD SINIFI (tasarım hakemi 08.10): çıplak birim literali ("... has" / "... adet" ile biten dizge) ve
# sunucu maden adının HAM basılması (X.MadenAdi; tek çevirici KurKaynakAdi.MadenAdi). Bicim.cs birimi
# DilEkranSozlugu'ndan alır; bu yüzden burada literal kalırsa EN modda Türkçe birim görünür.
kalipBirim = re.compile(r'(?:\\u00a0|\s)(?:has|adet)"')
kalipAyar = re.compile(r'Ayarlar\.Bul\([^)]*\)!?\??\.Ad\b')
kalipMaden = re.compile(r'(?<![\w.])[A-Za-z_]\w*(?:\[\d+\])?(?:\??\.[A-Za-z_]\w*(?:\[\d+\])?)*\??\.MadenAdi\b(?!\()')
for f in sorted(glob.glob(kok + '/Kuyumcu.Web/**/*.razor', recursive=True) + glob.glob(kok + '/Kuyumcu.Web/**/*.cs', recursive=True)):
    if '/obj/' in f or '/bin/' in f or f.endswith('/Servisler/RaporServisi.cs') or f.endswith('/Servisler/KarZararModelleri.cs'): continue
    t = open(f, encoding='utf-8').read()
    t = re.sub(r'@\*.*?\*@', lambda m: '\n' * m.group(0).count('\n'), t, flags=re.S)
    for ln, l in enumerate(t.split('\n'), 1):
        if l.lstrip().startswith('//'): continue
        for m in kalipBirim.finditer(l):
            n += 1; print('%s:%d: birim literali %s' % (os.path.relpath(f, kok), ln, m.group(0)), file=sys.stderr)
        for m in kalipAyar.finditer(l):
            n += 1; print('%s:%d: ham ayar adi %s' % (os.path.relpath(f, kok), ln, m.group(0)), file=sys.stderr)
        for m in kalipMaden.finditer(l):
            oncesi = l[:m.start()]
            if oncesi.rfind('KurKaynakAdi.MadenAdi(') > oncesi.rfind(')'): continue
            n += 1; print('%s:%d: ham MadenAdi %s' % (os.path.relpath(f, kok), ln, m.group(0)), file=sys.stderr)
print(n)
PYEOF_DE
}
de_say=$(en_turkce_baslik_say "$PWD" 2>/dev/null)
case "$de_say" in ''|*[!0-9]*) printf "  ✗ %-46s %s\n" "DIL-EKRAN: EN_MODDA_TURKCE_BASLIK" "ÖLÇÜLEMEDİ — sessiz yeşil DEĞİL"; HATA=1;;
  *) if [ "$de_say" -le 0 ]; then printf "  ✓ %-46s %s\n" "DIL-EKRAN: EN_MODDA_TURKCE_BASLIK" "0 (fail-closed; başlık + birim literali + ham maden adı)"
     else printf "  ✗ %-46s %s (tavan 0)\n" "DIL-EKRAN: EN_MODDA_TURKCE_BASLIK" "$de_say"; en_turkce_baslik_say "$PWD" 2>&1 >/dev/null | head -20; HATA=1; fi;;
esac
# <<< PAKET DIL-EKRAN

# >>> PAKET DIL-SUNUCU — SUNUCU_CIPLAK_TURKCE_ILETI (09.10; kurucu: "ingilizceye cevirince ... turkce kaliyor")
# Kuyumcu.Api'de KULLANICIYA GIDEN iletilerin (new *Exception(...), uyarilar.Add, eksikler.Add, ErrorMessage = ...,
# message = ..., alanlar[..] = ...) Turkce harfli STRING LITERAL'lerinden Kuyumcu.Core/I18n/SunucuIletiSozlugu.Veri.cs'de
# KARSILIGI OLMAYANLARIN sayisi. Karsiligi olmayan ileti EN istekte Turkce gider. Baslangic (sozluk oncesi) 1531 ileti
# yeri idi (sozluk bos olsaydi); simdi tavan 0 (fail-closed): yeni ileti yazan sozluge de satir ekler.
# Sablon kimligi: `{kasaId}` -> `{}`; sozluk anahtari ayni bicimdedir (Veri.cs satirlari "TR", "EN").
sunucu_ciplak_say() {
python3 - "$1" <<'PYEOF_DS'
import re, sys, glob, os
kok = sys.argv[1]
# Turkce: Turkce harf YA DA ASCII yazilmis ileti kokleri (zorunludur, bulunamadi, olabilir ...)
TR = re.compile(r'[çğıİöşüÇĞÖŞÜ]|\b(?:zorunlu|gerekli|olmal|olamaz|olabilir|bulunamad|gecersiz|secil|girilm|yazin|girin|kayitli|eksik|bos birak)')
CALL = re.compile(r'new\s+(?:[A-Za-z_][\w.]*\.)?(?:BadRequestException|ConflictException|NotFoundException|ForbiddenException|UnauthorizedException|ApiException|AlanCakismasiException|YasalParametreYokException|SubeIhlaliException|KiraciIhlaliException)\s*\(|\buyarilar\.Add\s*\(|\bAddUyari\s*\(|\bYaz\s*\(\s*ctx|\beksikler\.Add\s*\(|\bfarklar\.Add\s*\(|\b(?:message|ErrorMessage|mesaj|uyari|hata|duzeltmeYolu|jetonUyarisi|giderNotu|notu|farkTanimi|tanim|alanlar\[[^\]]+\])\s*=\s*(?=[^=])|\bconst\s+string\s+(?!KanalOperatoru|Maskelenmis|DogrulamaGerekcesi|VezneZorunlu)\w+\s*=\s*(?=[^=])')
def oku_dizge(s, i):
    """s[i] '"', '$"', '@"', '$@"' ile basliyor; (sablon, bitis) doner. delik -> {}"""
    j = i; interp = verb = False
    while s[j] in '$@':
        if s[j] == '$': interp = True
        else: verb = True
        j += 1
    assert s[j] == '"'; j += 1
    out = []
    while j < len(s):
        c = s[j]
        if verb and c == '"' and s[j+1:j+2] == '"': out.append('"'); j += 2; continue
        if not verb and c == '\\': out.append(s[j:j+2]); j += 2; continue
        if c == '"': return ''.join(out), j + 1
        if interp and c == '{':
            if s[j+1:j+2] == '{': out.append('{'); j += 2; continue
            d = 1; j += 1
            while j < len(s) and d:
                if s[j] == '"':
                    k = j + 1
                    while s[k] != '"': k += 2 if s[k] == '\\' else 1
                    j = k
                elif s[j] == '{': d += 1
                elif s[j] == '}': d -= 1
                j += 1
            out.append('{}'); continue
        if interp and c == '}' and s[j+1:j+2] == '}': out.append('}'); j += 2; continue
        out.append(c); j += 1
    return None, j
def ifade_sonu(s, i):
    d = 0
    while i < len(s):
        c = s[i]
        if c == '/' and s[i:i+2] == '//':
            k = s.find('\n', i); i = len(s) if k < 0 else k; continue
        if c == '"' or (c in '$@' and re.match(r'[$@]{1,2}"', s[i:i+3])):
            try:
                t, i2 = oku_dizge(s, i)
            except Exception:
                i += 1; continue
            if t is None: return i
            i = i2; continue
        if c in '([{': d += 1
        elif c in ')]}':
            if d == 0: return i
            d -= 1
        elif c in ',;' and d == 0: return i
        i += 1
    return i
def arglar(s, p):
    """p: '(' sonrasi. ilk seviye argumanlardaki dizge birlesimlerini doner"""
    d = 1; i = p; son = p; ciktilar = []; cur = []; bos = True
    while i < len(s) and d:
        c = s[i]
        if c == '/' and s[i:i+2] == '//':
            k = s.find('\n', i); i = len(s) if k < 0 else k; continue
        if c == '"' or (c in '$@' and re.match(r'[$@]{1,2}"', s[i:i+3])):
            try:
                t, i2 = oku_dizge(s, i)
            except Exception:
                i += 1; continue
            if t is None: break
            if cur and not re.fullmatch(r'\s*(?://[^\n]*\n\s*)?\+\s*(?://[^\n]*\n\s*)?', s[son:i]):
                ciktilar.append(''.join(cur)); cur = []
            cur.append(t); i = i2; son = i2; continue
        if c in '([{': d += 1
        elif c in ')]}': d -= 1
        elif c == ',' and d == 1:
            if cur: ciktilar.append(''.join(cur)); cur = []
        i += 1
    if cur: ciktilar.append(''.join(cur))
    return ciktilar

veri = open(kok + '/Kuyumcu.Core/I18n/SunucuIletiSozlugu.Veri.cs', encoding='utf-8').read()
def coz(m):
    return re.sub(r'\\(.)', lambda k: {'n': '\n'}.get(k.group(1), k.group(1)), m)
anahtarlar = {coz(m.group(1)) for m in re.finditer(r'^\s+\("((?:[^"\\]|\\.)*)", "(?:[^"\\]|\\.)*"\),\s*$', veri, re.M)}
n = 0
for f in sorted(glob.glob(kok + '/Kuyumcu.Api/**/*.cs', recursive=True)):
    if '/obj/' in f or '/bin/' in f or '/Migrations/' in f: continue
    s = open(f, encoding='utf-8').read()
    for m in CALL.finditer(s):
        ln = s.count('\n', 0, m.start()) + 1
        if s[s.rfind('\n', 0, m.start())+1:m.start()].lstrip().startswith('//'): continue
        govde = m.end()
        if not m.group(0).endswith('('):
            sub = s[govde:ifade_sonu(s, govde)] + ')'
        else:
            sub = s[govde:]
        for t in arglar(sub, 0):
            t = re.sub(r'\{\d+(?::[^{}]*)?\}', '{}', t)      # string.Format kalibi {0:N2} -> {}
            if re.search(r'\b(SELECT|UPDATE|FROM|WHERE|JOIN|GROUP BY|INSERT)\b', t): continue   # SQL sabiti ileti degil
            if TR.search(t) and t not in anahtarlar:
                n += 1; print('%s:%d: %s' % (os.path.relpath(f, kok), ln, t[:90]), file=sys.stderr)
print(n)
PYEOF_DS
}
sc_say=$(sunucu_ciplak_say "$PWD" 2>/dev/null)
case "$sc_say" in ''|*[!0-9]*) printf "  ✗ %-46s %s\n" "DIL-SUNUCU: SUNUCU_CIPLAK_TURKCE_ILETI" "ÖLÇÜLEMEDİ — sessiz yeşil DEĞİL"; HATA=1;;
  *) if [ "$sc_say" -le 0 ]; then printf "  ✓ %-46s %s\n" "DIL-SUNUCU: SUNUCU_CIPLAK_TURKCE_ILETI" "0 (fail-closed; her ileti SunucuIletiSozlugu'nda)"
     else printf "  ✗ %-46s %s (tavan 0) — SunucuIletiSozlugu.Veri.cs'ye TR/EN çifti ekleyin\n" "DIL-SUNUCU: SUNUCU_CIPLAK_TURKCE_ILETI" "$sc_say"; sunucu_ciplak_say "$PWD" 2>&1 >/dev/null | head -20; HATA=1; fi;;
esac
# <<< PAKET DIL-SUNUCU

echo
if [ "$HATA" = 0 ]; then echo "DENETİM TEMİZ."; else echo "DENETİM BAŞARISIZ — yukarıdaki ihlaller giderilmeli."; fi
exit $HATA

