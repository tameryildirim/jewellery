#!/usr/bin/env bash
# ═════════════════════════════════════════════════════════════════════════════
# YEDEK AL VE GERİ YÜKLEMEYİ DOĞRULA — ByGatech (paket T-3 Faz A)
#
# NEDEN: "Yedek alınıyor" bir iddia, "geri yüklenebiliyor" bir kanıttır.
# Bu betik yedeği alır, ŞİFRELER, dış kopyasını çıkarır, AYRI bir veritabanına
# geri yükler ve her tablonun sağlama toplamını (CHECKSUM TABLE) kaynakla
# karşılaştırır. Uyuşmazlık = sıfırdan farklı çıkış kodu (fail-closed kapı).
#
# ⚠ TEK YOL (MÜHÜR 6 / K3): yedek yolu YALNIZ budur. İkinci bir yedek betiği
#   yoktur; ortak işler dotnet/yedek-ortak.sh kitaplığındadır ve mysqldump
#   çağrısı tüm depoda TEK yerdedir (denetim.sh T3-4).
#
# ⚠ KAYNAK VERİTABANINA ASLA YAZILMAZ (tatbikat dışında hiçbir şema
#   oluşturulmaz; tatbikat şeması iş bitince DÜŞÜRÜLÜR).
#
# ⚠ GÖMÜLÜ PAROLA YOK (eski :30 satırı kaldırıldı — Mehmet veto koşulu).
#   Parola KUYUMCU_DB_SIFRE ortam değişkeninden gelir ve --defaults-extra-file
#   ile verilir; ARGV'ye hiç girmez ("ps aux" kanıtı runbook'ta).
#
# Kullanım:
#   ./yedek-al-ve-dogrula.sh                 # tam yedek + tatbikat (varsayılan)
#   ./yedek-al-ve-dogrula.sh tam             # yalnız şifreli tam yedek
#   ./yedek-al-ve-dogrula.sh elle            # tam yedek, tür=ELLE
#   ./yedek-al-ve-dogrula.sh talep           # elle yedek KUYRUĞUNU tüket
#   ./yedek-al-ve-dogrula.sh tatbikat        # son yedeği ayrı şemaya geri yükle
#
# Ortam değişkenleri (systemd: EnvironmentFile=/etc/kuyumcu/.env):
#   KUYUMCU_DB_SIFRE            (ZORUNLU — argv'ye girmez)
#   Yedek__SifrelemeAnahtari    (ZORUNLU — alan anahtarından AYRI)
#   YEDEK_DIZIN · YEDEK_DIS_HEDEF · YEDEK_SAKLAMA_GUN · YEDEK_SAKLAMA_ETKIN
#   KUYUMCU_VT · KUYUMCU_KAP · KUYUMCU_DB_SUNUCU · KUYUMCU_DB_PORT
# ═════════════════════════════════════════════════════════════════════════════
set -uo pipefail
cd "$(dirname "$0")"
# shellcheck source=./yedek-ortak.sh
# PAKET P-33 (temiz kurulum tatbikatında bulundu): yukarıdaki cd'den SONRA
# "$(dirname "$0")" göreli kalırsa (./dotnet/… ile çağrı) kitaplık
# dotnet/dotnet/ altında aranıyor ve betik hiç çalışmıyordu. cd zaten betiğin
# dizinine geçti: kitaplık buradan okunur.
. "./yedek-ortak.sh"

MOD="${1:-hepsi}"

# ── 1) TAM YEDEK ────────────────────────────────────────────────────────────
# Boru hattı: dump -> gzip -> gpg(AES-256) -> disk. Şifresiz ara dosya YOKTUR.
yedek_al() {   # $1 = tür (TAM|ELLE) -> yedek kaydı id'sini KAYIT_ID'ye yazar
  local tur="${1:-TAM}"
  anahtar_dogrula || return 1
  mkdir -p "$YEDEK_DIZIN" || { hata_ver "Yedek dizini oluşturulamadı: $YEDEK_DIZIN"; return 1; }

  local damga dosya
  damga="$(date +%Y%m%d-%H%M%S)"
  dosya="${YEDEK_DIZIN}/${YEDEK_VT}-${damga}.sql.gz.gpg"

  KAYIT_ID="$(kayit_ac "$tur" | tail -1 | tr -d ' ')"
  [ -z "$KAYIT_ID" ] && { hata_ver "yedek_kayitlari satırı açılamadı."; return 1; }

  local motor
  motor="$(innodb_disi_say)"
  bilgi "InnoDB olmayan tablo (ölçüm): ${motor:-?}"

  echo "=== 1/4 · Yedek alınıyor (şifreli) ==="
  set -o pipefail
  if ! dump_akisi | gzip -9 | sifrele > "$dosya"; then
    kayit_kapat "$KAYIT_ID" BASARISIZ "$(basename "$dosya")" 0 "" 0 YOK 0 "dump/şifreleme boru hattı başarısız"
    hata_ver "Dump ya da şifreleme başarısız — koşu BASARISIZ yazıldı."
    return 1
  fi

  local boyut sha tablo
  boyut="$(wc -c < "$dosya" | tr -d ' ')"
  if [ "${boyut:-0}" -lt 1024 ]; then
    kayit_kapat "$KAYIT_ID" BASARISIZ "$(basename "$dosya")" "$boyut" "" 0 YOK 0 "yedek dosyası şüpheli biçimde küçük"
    hata_ver "Yedek dosyası $boyut bayt — fail-closed."
    return 1
  fi
  sha="$(sha256_hesapla "$dosya")"
  tablo="$(my "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA='$YEDEK_VT';" | tr -d ' ')"
  printf '  %s (%s bayt · sha256 %s…)\n' "$dosya" "$boyut" "${sha:0:12}"

  echo "=== 2/4 · Dış kopya ==="
  local kopya
  kopya="$(dis_kopya_al "$dosya" | tail -1)"
  if [ "$kopya" != "KOPYALANDI" ]; then
    # FAIL-CLOSED 3: tek kopya yedek, yedek değildir.
    kayit_kapat "$KAYIT_ID" BASARISIZ "$(basename "$dosya")" "$boyut" "$sha" 1 "$kopya" "${tablo:-0}" "dış kopya alınamadı"
    hata_ver "Dış kopya alınamadı — koşu BASARISIZ."
    return 1
  fi
  bilgi "dış kopya: $YEDEK_DIS_HEDEF"

  kayit_kapat "$KAYIT_ID" BASARILI "$(basename "$dosya")" "$boyut" "$sha" 1 "$kopya" "${tablo:-0}" ""
  echo "=== 3/4 · Saklama ==="
  saklama_temizle
  SON_YEDEK_DOSYA="$dosya"
  return 0
}

# ── 2) TATBİKAT — gerçek geri yükleme provası ───────────────────────────────
# Son BAŞARILI yedek dosyası: sha256 doğrulanır, AYRI şemaya HAM geri yüklenir
# (dotnet ef database update KULLANILMAZ — ikinci migration yolu doğmaz),
# CHECKSUM karşılaştırılır, sonuç yedek_tatbikatlari'na yazılır ve şema DÜŞER.
tatbikat_kos() {
  anahtar_dogrula || return 1
  local satir dosya_adi kayit_id beklenen_sha dosya
  satir="$(my "SELECT id, dosya_adi, sha256 FROM yedek_kayitlari
               WHERE durum='BASARILI' AND dosya_adi IS NOT NULL
               ORDER BY baslangic DESC LIMIT 1;" "$YEDEK_VT")"
  kayit_id="$(echo "$satir" | awk '{print $1}')"
  dosya_adi="$(echo "$satir" | awk '{print $2}')"
  beklenen_sha="$(echo "$satir" | awk '{print $3}')"
  DOGRULANAN_KAYIT_ID="$kayit_id"   # P-77: talep satırına yazılır
  if [ -z "$dosya_adi" ]; then
    hata_ver "Doğrulanacak BAŞARILI yedek kaydı yok."
    return 1
  fi
  dosya="${YEDEK_DIZIN}/${dosya_adi}"
  [ -f "$dosya" ] || { hata_ver "Yedek dosyası bulunamadı: $dosya"; return 1; }

  local basla; basla="$(date +%s)"

  echo "=== 1/5 · sha256 doğrulaması ==="
  local sha; sha="$(sha256_hesapla "$dosya")"
  if [ "$sha" != "$beklenen_sha" ]; then
    hata_ver "sha256 UYUŞMADI — dosya bozulmuş ya da değiştirilmiş."
    my "UPDATE yedek_kayitlari SET dogrulama='UYUSMADI', dogrulama_tarih=UTC_TIMESTAMP() WHERE id='$kayit_id';" "$YEDEK_VT"
    return 1
  fi
  bilgi "sha256 eşleşti"

  echo "=== 2/5 · Kaynak parmak izi ==="
  local kaynak_izi hedef_izi
  kaynak_izi="$(mktemp)"; hedef_izi="$(mktemp)"
  parmak_izi "$YEDEK_VT" "$kaynak_izi"
  bilgi "$(wc -l < "$kaynak_izi" | tr -d ' ') tablo"
  if [ ! -s "$kaynak_izi" ]; then
    hata_ver "Kaynak parmak izi BOŞ — doğrulama yapılamaz."
    return 1
  fi

  echo "=== 3/5 · Ayrı şemaya HAM geri yükleme ==="
  my "DROP DATABASE IF EXISTS ${YEDEK_TATBIKAT_SEMA}; CREATE DATABASE ${YEDEK_TATBIKAT_SEMA};"
  if ! coz < "$dosya" | gunzip \
        | sed "s/\`${YEDEK_VT}\`/\`${YEDEK_TATBIKAT_SEMA}\`/g" \
        | grep -v '^CREATE DATABASE' | grep -v '^USE ' \
        | my_akis "$YEDEK_TATBIKAT_SEMA"; then
    hata_ver "Geri yükleme başarısız."
    my "DROP DATABASE IF EXISTS ${YEDEK_TATBIKAT_SEMA};"
    return 1
  fi
  bilgi "$(my "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA='${YEDEK_TATBIKAT_SEMA}';" | tr -d ' ') tablo geri yüklendi"

  echo "=== 4/5 · Sağlama toplamları ==="
  parmak_izi "$YEDEK_TATBIKAT_SEMA" "$hedef_izi"
  local cikti sonuc=0
  cikti="$(karsilastir "$kaynak_izi" "$hedef_izi" "$YEDEK_VT" "$YEDEK_TATBIKAT_SEMA")" || sonuc=1
  echo "$cikti" | sed 's/^/  /'

  local tablo uyusan uyusmayan adlar
  tablo="$(echo "$cikti" | sed -n 's/^TABLO=//p')"
  uyusan="$(echo "$cikti" | sed -n 's/^UYUSAN=//p')"
  uyusmayan="$(echo "$cikti" | sed -n 's/^UYUSMAYAN=//p')"
  adlar="$(echo "$cikti" | sed -n 's/^ADLAR=//p')"

  # ── CANLI VERİTABANI TUZAĞI (2026-09-17 ölçümü) ──────────────────────────
  # Kaynak parmak izi dump'tan SONRA, YAŞAYAN veritabanından alınır: dump
  # anıyla karşılaştırma anı arasında yazılan tablo (denetim_kayitlari,
  # kur_snapshots, yedeğin kendi izi) doğal olarak uyuşmaz. Bunu "yedek bozuk"
  # diye raporlamak da, görmezden gelmek de yanlış olurdu.
  # ÖLÇÜLEBİLİR AYRIM: information_schema.UPDATE_TIME dump başlangıcından
  # SONRAYSA tablo DEĞİŞKENDİR (yedek kusuru değil); değilse GERÇEK uyuşmazlıktır
  # ve koşu BAŞARISIZ olur (fail-closed yönü korunur: yanlış YEŞİL olmaz).
  local dump_basi degisken=0 gercek=0 gercek_adlar="" degisken_adlar="" t
  dump_basi="$(my "SELECT baslangic FROM yedek_kayitlari WHERE id='$kayit_id';" "$YEDEK_VT" | tr -d '\r')"
  if [ -n "$adlar" ]; then
    while IFS= read -r t; do
      [ -z "$t" ] && continue
      local sonra
      # ⚠ information_schema.UPDATE_TIME ÖNBELLEKLİDİR: MySQL 8'de
      # information_schema_stats_expiry varsayılanı 86400 sn'dir ve BAYAT değer
      # tabloyu "dump'tan sonra yazılmamış" gösterip sahte KIRMIZI üretir
      # (2026-09-18 ölçümü). Oturumda süre sıfırlanır — değer TAZE okunur.
      sonra="$(my "SET SESSION information_schema_stats_expiry=0;
                   SELECT COUNT(*) FROM information_schema.TABLES
                   WHERE TABLE_SCHEMA='$YEDEK_VT' AND TABLE_NAME='$t'
                     AND UPDATE_TIME IS NOT NULL AND UPDATE_TIME >= '$dump_basi';" | tr -d ' ')"
      if [ "${sonra:-0}" = "1" ]; then
        degisken=$((degisken + 1)); degisken_adlar="${degisken_adlar}${t},"
      else
        gercek=$((gercek + 1)); gercek_adlar="${gercek_adlar}${t},"
      fi
    done <<< "$(echo "$adlar" | tr ',' '\n')"
    bilgi "uyuşmayan $uyusmayan tablo: $degisken değişken (dump sonrası yazıldı) · $gercek gerçek"
  fi
  uyusmayan="$gercek"
  uyusan=$((${tablo:-0} - gercek))
  adlar="${gercek_adlar%,}"
  sonuc=$([ "$gercek" = "0" ] && echo 0 || echo 1)

  echo "=== 5/5 · Temizlik ve rapor ==="
  my "DROP DATABASE IF EXISTS ${YEDEK_TATBIKAT_SEMA};"
  local kalan dusuruldu
  kalan="$(my "SELECT COUNT(*) FROM information_schema.SCHEMATA WHERE SCHEMA_NAME='${YEDEK_TATBIKAT_SEMA}';" | tr -d ' ')"
  dusuruldu=$([ "${kalan:-1}" = "0" ] && echo 1 || echo 0)

  local bitti sure dk durum_kelime rapor
  bitti="$(date +%s)"; sure=$((bitti - basla)); dk=$(( (sure + 59) / 60 ))
  if [ "${uyusmayan:-1}" = "0" ] && [ "$sonuc" = "0" ]; then
    durum_kelime="BASARILI"
  else
    durum_kelime="BASARISIZ"
  fi
  # ⚠ Rapor YALNIZ tablo ADI taşır; hiçbir satır/alan DEĞERİ yazılmaz.
  rapor="uyusmayan_tablolar=${adlar} · degisken_tablolar=${degisken_adlar%,}"

  my "INSERT INTO yedek_tatbikatlari
      (id,tarih,yedek_kaydi_id,tablo_sayisi,uyusan,uyusmayan,sonuc,sure_sn,rapor,sema_dusuruldu,rto_dakika_olculen)
      VALUES ('t$(date -u +%Y%m%d%H%M%S)$(printf '%04d' $((RANDOM % 10000)))',UTC_TIMESTAMP(),'$kayit_id',
      ${tablo:-0},${uyusan:-0},${uyusmayan:-0},'$durum_kelime',$sure,'$(printf '%s' "$rapor" | sed "s/'/''/g" | cut -c1-4000)',
      $dusuruldu,$dk);" "$YEDEK_VT"

  if [ "$durum_kelime" = "BASARILI" ]; then
    my "UPDATE yedek_kayitlari SET dogrulama='DOGRULANDI', dogrulama_tarih=UTC_TIMESTAMP() WHERE id='$kayit_id';" "$YEDEK_VT"
    echo "  GERİ YÜKLEME DOĞRULANDI — gerçek uyuşmazlık 0 · ${uyusan}/${tablo} tablo eşleşti · ${degisken} tablo dump sonrası yazıldığı için değişken (${sure} sn)."
  else
    # FAIL-CLOSED 1: uyuşmazlıkta koşu doğrulanmaz ve dosya saklamadan MUAFTIR.
    my "UPDATE yedek_kayitlari SET dogrulama='UYUSMADI', dogrulama_tarih=UTC_TIMESTAMP() WHERE id='$kayit_id';" "$YEDEK_VT"
    hata_ver "GERİ YÜKLEME DOĞRULANAMADI: ${adlar}"
  fi

  printf '' > "$kaynak_izi"; printf '' > "$hedef_izi"
  command rm -f "$kaynak_izi" "$hedef_izi"
  [ "$durum_kelime" = "BASARILI" ] || return 1
  return 0
}

# ── 3) TALEP KUYRUĞU ────────────────────────────────────────────────────────
# API süreç başlatmaz; bekleyen satırları BU betik tüketir (60 sn'lik timer).
# PAKET P-77: satırın TÜRÜ işi seçer — YEDEK → elle yedek al; DOGRULAMA → son
# BAŞARILI yedeği ayrı şemaya geri yükleyip doğrula (tatbikat_kos). Doğrulama
# yedek DOSYASINI yalnız OKUR; silmez, değiştirmez. Satırın yedek_kaydi_id'si
# doğrulanan koşunun kimliğidir.
talep_kuyrugu() {
  local talepler t tur
  talepler="$(my "SELECT id, tur FROM yedek_talepleri WHERE durum='BEKLIYOR' ORDER BY olusturuldu LIMIT 20;" "$YEDEK_VT")"
  if [ -z "$talepler" ]; then
    bilgi "bekleyen elle yedek/doğrulama talebi yok"
    return 0
  fi
  local sonuc=0
  while IFS=$'\t' read -r t tur; do
    [ -z "$t" ] && continue
    if [ "$tur" = "DOGRULAMA" ]; then
      bilgi "doğrulama talebi işleniyor: $t"
      DOGRULANAN_KAYIT_ID=""
      if tatbikat_kos; then
        my "UPDATE yedek_talepleri SET durum='ISLENDI', islendi=UTC_TIMESTAMP(), yedek_kaydi_id=NULLIF('$DOGRULANAN_KAYIT_ID','') WHERE id='$t';" "$YEDEK_VT"
      else
        my "UPDATE yedek_talepleri SET durum='HATA', islendi=UTC_TIMESTAMP(), yedek_kaydi_id=NULLIF('$DOGRULANAN_KAYIT_ID','') WHERE id='$t';" "$YEDEK_VT"
        sonuc=1
      fi
      continue
    fi
    bilgi "talep işleniyor: $t"
    if yedek_al ELLE; then
      my "UPDATE yedek_talepleri SET durum='ISLENDI', islendi=UTC_TIMESTAMP(), yedek_kaydi_id='$KAYIT_ID' WHERE id='$t';" "$YEDEK_VT"
    else
      my "UPDATE yedek_talepleri SET durum='HATA', islendi=UTC_TIMESTAMP() WHERE id='$t';" "$YEDEK_VT"
      sonuc=1
    fi
  done <<< "$talepler"
  return $sonuc
}

# ── Giriş ───────────────────────────────────────────────────────────────────
KAYIT_ID=""
SON_YEDEK_DOSYA=""
DOGRULANAN_KAYIT_ID=""
kimlik_kur || exit 1

case "$MOD" in
  tam)       yedek_al TAM; exit $? ;;
  elle)      yedek_al ELLE; exit $? ;;
  talep)     talep_kuyrugu; exit $? ;;
  tatbikat)  tatbikat_kos; exit $? ;;
  hepsi)     yedek_al TAM && tatbikat_kos; exit $? ;;
  *)
    hata_ver "Bilinmeyen mod: $MOD (tam | elle | talep | tatbikat | hepsi)"
    exit 2 ;;
esac
