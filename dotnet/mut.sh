#!/usr/bin/env bash
# ═════════════════════════════════════════════════════════════════════════════
# MUTASYON KANITI BETİĞİ — B-53 (P-02 ile SÜRÜMLENDİ, 2026-09-18).
#
# NEDEN BURADA: bu betik bugüne dek yalnız oturum geçici dizininde (/private/tmp)
# duruyordu. Oturum kapanınca kayboluyor ve "kuralı boz → test kırmızı" kanıt
# zinciri yeniden üretilemiyordu (B-53). Artık depoda.
#
# KULLANIM:
#   ./dotnet/mut.sh <etiket> <dosya-göreli-yol> <eski-metin-dosyası> <yeni-metin-dosyası> <test-filtresi>
#   Yollar dotnet/ dizinine görelidir. <eski-metin-dosyası> içeriği kaynakta
#   TAM OLARAK BİR KEZ geçmelidir; geçmezse mutasyon UYGULANMAZ (ESLESME=n).
#
# ⚠ ARTIK BIRAKIR: yedek <dosya>.mutorj olarak KALIR ve SİLİNMEZ — TurkReport
#   hook'u 'rm'/'mv' komutlarını engeller. Silme kurucu kalemidir (K-4).
#   Bu yüzden denetim.sh'ın P-02 bloğunda MUTORJ_ARTIK sayacı vardır: artık
#   sayısı yalnız AZALABİLİR.
#
# ⚠ BİTİŞTE TEMİZ DERLEME ŞART: son 'dotnet test' mutant kaynağı derlemiştir;
#   kaynak geri kopyalansa bile bin/ altındaki ikili MUTANTTIR. Canlı
#   doğrulamadan önce 'dotnet build Kuyumcu.sln' koş (SONRAKI § 6 tuzağı).
# ═════════════════════════════════════════════════════════════════════════════
set -uo pipefail
KOK="$(cd "$(dirname "$0")" && pwd)"

if [ "$#" -ne 5 ]; then
  echo "kullanım: $0 <etiket> <dosya> <eski-metin-dosyası> <yeni-metin-dosyası> <test-filtresi>" >&2
  exit 2
fi

etiket="$1"; f="$KOK/$2"; eski="$3"; yeni="$4"; filtre="$5"
yedek="$f.mutorj"

[ -f "$f" ]    || { echo "HATA: kaynak yok: $f" >&2; exit 2; }
[ -f "$eski" ] || { echo "HATA: eski metin dosyası yok: $eski" >&2; exit 2; }
[ -f "$yeni" ] || { echo "HATA: yeni metin dosyası yok: $yeni" >&2; exit 2; }

cp "$f" "$yedek"

# Tek eşleşme zorunluluğu: 0 eşleşme = mutasyon hedefi kaymış, >1 eşleşme =
# hangi kuralın kırıldığı belirsiz. İkisi de kanıtı geçersiz kılar.
perl -0777 -e '
  my ($p,$ea,$eb)=@ARGV;
  local $/; open(F,"<",$p) or die; my $s=<F>; close F;
  open(F,"<",$ea) or die; my $a=<F>; close F;
  open(F,"<",$eb) or die; my $b=<F>; close F;
  my $n = () = $s =~ /\Q$a\E/g;
  if ($n != 1) { print STDERR "ESLESME=$n (tam 1 olmalı)\n"; exit 3; }
  $s =~ s/\Q$a\E/$b/;
  open(F,">",$p) or die; print F $s; close F;
' "$f" "$eski" "$yeni"

if [ $? -ne 0 ]; then
  echo "=== $etiket ==="; echo "   MUTASYON UYGULANAMADI (tek eşleşme yok)"
  cp "$yedek" "$f"; exit 1
fi

cikti=$(cd "$KOK" && dotnet test Kuyumcu.Tests/Kuyumcu.Tests.csproj --nologo -v n --filter "$filtre" 2>&1)

# Kaynağı HER HÂLDE geri al (test yarıda ölse bile mutant kalmasın).
cp "$yedek" "$f"

echo "=== $etiket ==="
echo "$cikti" | grep -E "Başarısız:|Failed:|error CS" | head -3 | sed 's/^/   /'
echo "$cikti" | grep -E "^ *(Başarısız|Failed) " | head -8 | sed 's/^ */   KIRMIZI: /'

# GERİ ALMA KANITI (Anayasa md.1): "geri aldım" iddiası cmp ile kanıtlanır.
if cmp -s "$f" "$yedek"; then
  echo "   ✓ geri alındı (cmp: $2 ≡ $2.mutorj)"
else
  echo "   ✗ GERİ ALINAMADI — kaynak hâlâ MUTANT: $f" >&2
  exit 4
fi

# Hiçbir testi kırmayan mutasyon kanıt değil, AÇIKTIR.
if ! echo "$cikti" | grep -qE "^ *(Başarısız|Failed) |error CS"; then
  echo "   ⚠ HİÇBİR TEST KIRILMADI — TEST AÇIĞI (testi güçlendir, mutasyonu tekrarla)"
  exit 5
fi
