#!/bin/zsh
# ── NÖBETÇİ ZİNCİRİ (kurucu talimatı, 2026-09-17) ────────────────────────────
# Üç seviye HALKA olarak birbirini denetler; hiçbiri tek başına ölünce zincir kopmaz:
#   seviye 1 → koşan işi (dalga/QA/kurul iş akışı) izler; durmuşsa DEVAM ETTİRİR
#   seviye 2 → seviye 1'i izler; kalbi durmuşsa YENİDEN BAŞLATIR
#   seviye 3 → seviye 2'yi izler; kalbi durmuşsa YENİDEN BAŞLATIR
#   seviye 1 → ayrıca seviye 3'ü izler (halka kapanır)
# Kullanım: nohup zsh betikler/nobetci.sh <1|2|3> >/dev/null 2>&1 &
#
# KURUCU TALİMATI (2026-09-17): "nöbetçi 'iş durdu' diye alarm yazmasın, işleri
# ben 'devam et' demişim gibi devam ettirsin." Bu yüzden nöbetçi durmayı
# yakaladığında BEKLEMEZ; sırayla şunları yapar:
#   1) kendi başına düzeltebileceğini düzeltir (inceleme sunucusu düşmüşse kaldırır,
#      sahipsiz kalmış dotnet derleme süreçlerini temizler),
#   2) DEVAM dosyasına "şu koşumu şu kimlikle sürdür" emrini yazar; orkestratörün
#      10 saniyede bir yoklayan nöbeti bundan UYANIR ve koşumu resumeFromRunId ile
#      kaldığı yerden sürdürür (kayıp yok, sıfırdan başlatma yok).
# DÜRÜST SINIR (Madde 1): kabuk betiği Workflow'u kendi başına başlatamaz — onu
# yalnız orkestratör oturumu çağırabilir. Bu yüzden "devam" iki adımda olur ve
# gecikme saniyeler mertebesindedir; kurucunun elini gerektiren hiçbir adım yoktur.
set -u
KAYIT=/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/dokumanlar/09-uygulama-kayitlari
HAM=$KAYIT/ham
# YAYIN DİZİNİ: TEK KAYNAK (P-42). Bu dosyada iki ayrı yerde (SON-DURUM satırı ve
# 5199 kurtarması) oturuma özgü yol YAZILIYDI; artık yalnız $YAYIN_DIZINI var.
source "$KAYIT/betikler/yayin-yolu.sh"
SEVIYE=${1:?kullanım: nobetci.sh <1|2|3>}
ARALIK=600      # 10 dk — kurucunun 20/09 talebi: kontroller sohbete de aksın
# ⚠ BAYAT > ARALIK olmak ZORUNDA. 2026-09-21'de ölçülen hata: ARALIK 300→600'e
#   çıkarılırken BAYAT 480'de kalmıştı; 8 dk eşiği 10 dk'lık turdan KISA olduğu
#   için seviye 2 ve 3 her turun arasında "ölmüş" görünüyordu ve kurucunun sabah
#   baktığı SON-DURUM.txt "2 DURDU · 3 DURDU" diye YANLIŞ yazıyordu (süreçler
#   pgrep'te canlıydı). Kural: BAYAT = ARALIK + en az bir tur payı.
BAYAT=$((ARALIK + 300))   # 15 dk — nöbetçi kalbi bu kadar eskiyse o nöbetçi ölmüş sayılır
SESSIZ=900      # 15 dk — iş ajanı bu kadar kıpırdamadıysa iş durmuş sayılır
KALP=$HAM/nobetci-$SEVIYE.kalp
GUNLUK=$HAM/nobetci-$SEVIYE.log
ALARM=$HAM/NOBETCI-DEVAM   # orkestratöre "kaldığın yerden sürdür" emri
AKTIF=$HAM/AKTIF-KOSUM   # orkestratörün yazdığı: "<koşum dizini>|<görev kimliği>|<açıklama>"

yaz() { printf -- '%s  %s\n' "$(date '+%d/%m %H:%M:%S')" "$1" >> "$GUNLUK"; }
yas() { [ -e "$1" ] && echo $(( $(date +%s) - $(stat -f %m "$1") )) || echo 999999; }

# ── KALAN İŞ SAYISI ───────────────────────────────────────────────────────────
# ham/KALAN-PLAN'daki her satır bir iş (dalga|kod|ad). Bir iş, HERHANGİ bir
# koşum günlüğünde "paket_no":"<kod>" + "karar":"GECTI" ile görünüyorsa BİTMİŞ
# sayılır. Böylece sayı kendi kendini günceller; elle bakım gerekmez.
KOSUMLAR=/Users/ramazanakkocan/.claude/projects/-Users-ramazanakkocan-Desktop-ByGatech-projeler-jewellery/f8597a15-bda0-4765-a57f-77f2b6380580/subagents/workflows
# ELDEN KAPANAN: ajan kararı olmayan ama orkestratörün kapı kanıtıyla kapattığı
# paketler (ham/ELDEN-KAPANAN). 2026-09-21'de ölçüldü: P-02 ve P-05 bitmişken
# sayaç onları KALDI sayıyordu ve kurucunun ekranı 17 yerine 19 gösteriyordu.
# Liste depoda, satır satır kanıtlı ve gözle görülür — sessiz bir istisna DEĞİL.
ELDEN=$HAM/ELDEN-KAPANAN
elden_mi() { [ -r "$ELDEN" ] && grep -q "^$1|" "$ELDEN"; }

kalan_sayisi() {
  local toplam=0 biten=0 kod
  while IFS='|' read -r _ kod _; do
    [ -z "$kod" ] && continue
    case "$kod" in '#'*) continue;; esac
    toplam=$((toplam+1))
    if elden_mi "$kod"; then biten=$((biten+1)); continue; fi
    if grep -lq "\"paket_no\":\"$kod\"" "$KOSUMLAR"/*/journal.jsonl 2>/dev/null &&
       grep -q "\"paket_no\":\"$kod\"[^}]*\"karar\":\"GECTI\"\|\"karar\":\"GECTI\"[^}]*\"paket_no\":\"$kod\"" "$KOSUMLAR"/*/journal.jsonl 2>/dev/null; then
      biten=$((biten+1))
    fi
  done < "$HAM/KALAN-PLAN"
  echo $((toplam-biten))
}

# ── KURUCU EKRANI: tek bakışta durum (sabah bakınca görülecek) ────────────────
EKRAN=/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery/SON-DURUM.txt
ekran_yaz() {        # $1 = DEVAM EDİYOR | DURDU→DEVAM ETTİRİLDİ   $2 = açıklama
  local kalan son_test
  kalan=$(kalan_sayisi)
  son_test=$(grep -oh '"test_toplam":[0-9]*' "$KOSUMLAR"/*/journal.jsonl 2>/dev/null | sed 's/.*://' | sort -n | tail -1)
  {
    printf 'KUYUMCU ERP — SON DURUM\n'
    printf 'Güncellendi: %s\n\n' "$(date '+%d.%m.%Y · saat %H:%M')"
    printf '%s\n' "$1"
    printf 'Şu an: %s\n' "$2"
    printf 'Kalan iş: %s\n' "$kalan"
    # ── KURUCU 5199'da NEYİ GÖRÜYOR? (21 Eylül talebi) ──────────────────────
    # Kurucu "dropdown'ı göremedim" dedi: iş ağaçta bitmiş olabilir ama ekranına
    # ancak YENİDEN YAYIN alınınca düşer. Bu satır o farkı görünür yapar.
    YAYIN_DLL="$YAYIN_DIZINI/Kuyumcu.Api.dll"
    if [ -f "$YAYIN_DLL" ]; then
      printf 'Ekranında (5199) gördüğün sürüm: %s · %s\n' \
        "$(date -r "$YAYIN_DLL" '+%d.%m %H:%M')" \
        "$(cat "$HAM/YAYIN-ETIKET" 2>/dev/null || echo 'etiketsiz')"
      printf '  (ağaçta biten iş, YENİ YAYIN alınana dek bu ekrana düşmez)\n'
    fi
    printf 'Test sayısı: %s\n' "${son_test:-—}"
    printf 'Nöbetçiler: 1 %s · 2 %s · 3 %s\n' \
      "$( [ "$(yas "$HAM/nobetci-1.kalp")" -lt "$BAYAT" ] && echo çalışıyor || echo DURDU )" \
      "$( [ "$(yas "$HAM/nobetci-2.kalp")" -lt "$BAYAT" ] && echo çalışıyor || echo DURDU )" \
      "$( [ "$(yas "$HAM/nobetci-3.kalp")" -lt "$BAYAT" ] && echo çalışıyor || echo DURDU )"
    printf '\nGeçmiş (son 12 kontrol):\n'
    tail -12 "$HAM/nobetci-1.log" 2>/dev/null | sed 's/^/  /'
  } > "$EKRAN"
}

baslat_nobetci() {   # $1 = seviye
  pgrep -f "nobetci.sh $1" >/dev/null 2>&1 && return 0
  nohup zsh "$KAYIT/betikler/nobetci.sh" "$1" >/dev/null 2>&1 &
  yaz "SEVİYE $1 ÖLMÜŞ → yeniden başlatıldı (pid $!)"
}

# ── HESAP LİMİTİ KALKANI (2026-09-23, ölçülmüş arıza) ───────────────────────
#   22.09 06:47'de iki şerit de "You've hit your weekly limit · resets Sep 23 at
#   9am" hatasıyla durdu. Nöbetçi bunu "ajan takıldı" sanıp 29 saat boyunca 12 kez
#   boşa DEVAM emri yazdı. Limit bizim tarafımızdan aşılamaz: emir yazmak işe
#   yaramaz, yalnız kaydı kirletir. Kural: son ajan dosyasında limit izi varsa
#   emir YAZILMAZ, "BEKLENİYOR" diye kaydedilir. Şerit yeni koşumla yeniden
#   başlatıldığında dizin de yenilenir, iz kendiliğinden temizlenir.
limit_bekliyor() {   # $1 = koşum dizini · dönüş 0 = limit var, DEVAM emri yazma
  local YENI
  YENI=$(ls -t "$1"/agent-*.jsonl 2>/dev/null | head -1)
  [ -z "${YENI:-}" ] && return 1
  tail -c 40000 "$YENI" 2>/dev/null \
    | grep -qiE "weekly limit|usage limit|hit your [a-z]+ limit|rate_limit_error" && return 0
  return 1
}

# ── AĞ KALKANI (2026-09-23, kurucu uyarısı) ─────────────────────────────────
#   Kurucu: "sadece internet gidebilir, ona dikkat edelim."
#   Ağ yokken ajan isteği düşer; DEVAM emri yazmak işe yaramaz, yalnız kaydı
#   kirletir ve gerçek sebebi gizler (haftalık limitte aynı hatayı yaptık).
#   Kural: ağ yoksa emir YAZILMAZ, "BEKLENİYOR (ağ yok)" diye kaydedilir.
#   Ölçüm yerel değil DIŞ uçla yapılır; 5199 ayakta olsa da internet kopuk olabilir.
ag_yok() {           # dönüş 0 = ağ YOK
  curl -s -o /dev/null --max-time 6 https://api.anthropic.com/ 2>/dev/null && return 1
  curl -s -o /dev/null --max-time 6 https://www.apple.com/ 2>/dev/null && return 1
  return 0
}

# ── TEKRAR KALKANI: emir yazıyoruz ama hareket yoksa geri çekil ─────────────
#   Emir yazıldıktan sonra ajan hareketi başlamıyorsa aynı emri 10 dk'da bir
#   tekrarlamak kaydı şişirir. Üçüncü boş emirden sonra saatte bir emre düşülür.
BOS_EMIR_DIZIN=$HAM/nobetci-bos-emir
tekrar_kalkani() {   # $1 = koşum dizini · dönüş 0 = emir yazılabilir
  mkdir -p "$BOS_EMIR_DIZIN" 2>/dev/null
  local ANAHTAR SAYAC_D SON_D SIMDI
  ANAHTAR=$(printf '%s' "$1" | tr -c 'A-Za-z0-9' '_')
  SAYAC_D=$BOS_EMIR_DIZIN/$ANAHTAR.sayac
  SON_D=$BOS_EMIR_DIZIN/$ANAHTAR.son
  SIMDI=$(date +%s)
  local SAYAC=0 SON=0
  [ -f "$SAYAC_D" ] && SAYAC=$(cat "$SAYAC_D" 2>/dev/null || echo 0)
  [ -f "$SON_D" ]  && SON=$(cat "$SON_D" 2>/dev/null || echo 0)
  if [ "$SAYAC" -ge 3 ] && [ $((SIMDI - SON)) -lt 3600 ]; then
    return 1
  fi
  printf '%s' "$((SAYAC + 1))" > "$SAYAC_D"
  printf '%s' "$SIMDI" > "$SON_D"
  return 0
}
tekrar_kalkanini_sifirla() {   # ajan hareket ettiğinde çağrılır
  mkdir -p "$BOS_EMIR_DIZIN" 2>/dev/null
  local ANAHTAR
  ANAHTAR=$(printf '%s' "$1" | tr -c 'A-Za-z0-9' '_')
  printf '0' > "$BOS_EMIR_DIZIN/$ANAHTAR.sayac" 2>/dev/null
}

# ── DEVAM ETTİR: önce kendi elinden geleni yap, sonra orkestratörü uyandır ────
devam_ettir() {      # $1 = sebep
  yaz "DEVAM ETTİRİLİYOR: $1"

  # (a) İnceleme sunucusu düşmüşse kaldır — bu betiğin tek başına yapabileceği iş.
  if ! curl -s -o /dev/null --max-time 5 http://localhost:5199/ 2>/dev/null; then
    YAYIN="$YAYIN_DIZINI"
    if [ -f "$YAYIN/Kuyumcu.Api.dll" ]; then
      ( nohup zsh "$KAYIT/betikler/qa-sunucu.sh" >/dev/null 2>&1 & )
      yaz "  → 5199 inceleme sunucusu düşmüştü, yeniden kaldırıldı"
    fi
  fi

  # (b) Sahipsiz kalmış derleme/test süreçleri: 45 dakikadan eski dotnet build/test
  #     sonraki denemeyi de boğar; temizlenir (yalnız build/test, sunucu değil).
  for P in $(pgrep -f "dotnet (build|test)" 2>/dev/null); do
    BAS=$(ps -o etimes= -p "$P" 2>/dev/null | tr -d ' ')
    [ -n "$BAS" ] && [ "$BAS" -gt 2700 ] && kill "$P" 2>/dev/null && yaz "  → $((BAS/60)) dk'dır asılı kalan dotnet süreci (pid $P) kapatıldı"
  done

  # (c) Orkestratöre DEVAM emri: koşumu kaldığı yerden sürdür (resumeFromRunId).
  printf -- '%s | DEVAM ET | %s | koşum: %s | görev: %s | sebep: %s\n' \
    "$(date '+%d/%m %H:%M:%S')" "${ACIK:-iş}" "${DIZIN:-?}" "${GOREV:-?}" "$1" >> "$ALARM"
  yaz "  → orkestratöre DEVAM emri yazıldı ($ALARM)"
  ekran_yaz "DURDU → DEVAM ETTİRİLDİ" "${ACIK:-iş} (sebep: $1)"
}

# Tek tur: "nobetci.sh <seviye> tek" — kurucu "şimdi kontrol et" dediğinde
# döngüyü beklemeden bir kontrol yapar, ekranı tazeler ve çıkar.
tur_at() {
  date '+%d/%m %H:%M:%S' > "$KALP"

  case $SEVIYE in
    1)
      # ── İŞİ DENETLE: aktif koşum dizinindeki en yeni ajan dosyasının yaşı ──
      # ── İŞİ DENETLE: AKTIF-KOSUM'daki HER ŞERİT ayrı ayrı (çok şeritli koşum) ──
      if [ -s "$AKTIF" ]; then
        CANLI=0; DURAN=0
        while IFS='|' read -r DIZIN GOREV ACIK; do
          [ -z "${DIZIN:-}" ] && continue
          if [ ! -d "$DIZIN" ]; then devam_ettir "AKTİF-KOŞUM dizini bulunamadı: $DIZIN"; DURAN=$((DURAN+1)); continue; fi
          YENI=$(ls -t "$DIZIN"/agent-*.jsonl 2>/dev/null | head -1)
          YAS=$(yas "${YENI:-$DIZIN/journal.jsonl}")
          if [ "$YAS" -gt "$SESSIZ" ]; then
            if ag_yok; then
              yaz "BEKLENİYOR · $ACIK · İNTERNET YOK — DEVAM emri YAZILMADI (ağ gelince sürer)"
              ekran_yaz "AĞ YOK" "$ACIK (internet kopuk; emir yazılmıyor)"
            elif limit_bekliyor "$DIZIN"; then
              yaz "BEKLENİYOR · $ACIK · son ajan hatası HESAP LİMİTİ — DEVAM emri YAZILMADI (bizde çözümü yok)"
              ekran_yaz "LİMİT BEKLENİYOR" "$ACIK (hesap limiti; emir yazılmıyor)"
            elif tekrar_kalkani "$DIZIN"; then
              devam_ettir "$ACIK · son ajan hareketi $((YAS/60)) dk önce (eşik $((SESSIZ/60)) dk)"
            else
              yaz "BEKLENİYOR · $ACIK · 3 emre rağmen hareket yok — saatte bir emre düşüldü"
            fi
            DURAN=$((DURAN+1))
          else
            CANLI=$((CANLI+1))
            tekrar_kalkanini_sifirla "$DIZIN"
            yaz "DEVAM EDİYOR · $ACIK · son hareket $((YAS/60)) dk önce"
          fi
        done < "$AKTIF"
        SERITLER=$(cut -d'|' -f3 "$AKTIF" | paste -sd' + ' -)
        if [ "$DURAN" -eq 0 ]; then
          yaz "DEVAM EDİYOR · kalan iş: $(kalan_sayisi) · şeritler: $SERITLER"
          ekran_yaz "DEVAM EDİYOR" "$SERITLER"
        fi
      else
        yaz "aktif koşum yok (AKTIF-KOSUM boş) — yalnız kalp atışı"
      fi
      # ── HALKAYI KAPAT: seviye 3'ü denetle ──
      [ "$(yas "$HAM/nobetci-3.kalp")" -gt "$BAYAT" ] && baslat_nobetci 3
      ;;
    2)
      [ "$(yas "$HAM/nobetci-1.kalp")" -gt "$BAYAT" ] && baslat_nobetci 1
      yaz "seviye 1 kalbi $(yas "$HAM/nobetci-1.kalp") sn"
      ;;
    3)
      [ "$(yas "$HAM/nobetci-2.kalp")" -gt "$BAYAT" ] && baslat_nobetci 2
      # yedek emniyet: ikisi birden ölmüşse birinciyi de ayağa kaldır
      [ "$(yas "$HAM/nobetci-1.kalp")" -gt "$BAYAT" ] && baslat_nobetci 1
      yaz "seviye 2 kalbi $(yas "$HAM/nobetci-2.kalp") sn"
      ;;
  esac

}

[ "${2:-}" = tek ] && { tur_at; exit 0; }

yaz "nöbet başladı (pid $$, aralık ${ARALIK}sn)"
while true; do
  tur_at
  sleep "$ARALIK"
done
