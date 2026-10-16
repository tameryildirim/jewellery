#!/usr/bin/perl
# ═════════════════════════════════════════════════════════════════════════════
# PAKET OLCEK-YUK — Take'siz ToListAsync/ToDictionaryAsync deyimlerini SINIFLANDIRIR.
# TEK KAYNAK: denetim.sh TB-19 sayacı bu betiği ÇAĞIRIR (--say kipi); döküm aynı kurallarla.
#   find dotnet/Kuyumcu.Api -name '*.cs' | grep -v '/obj/\|/bin/\|/Migrations/' \
#     | xargs perl tolist-siniflandir.pl > siniflandirma.tsv
#   ... | xargs perl tolist-siniflandir.pl --say   → "SINIRSIZ BUYUYEN" (iki sayı, tek satır)
# Çıktı: dosya<TAB>satır<TAB>sınıf<TAB>deyimin son 140 karakteri
#
# t2 (doğrulayıcı DÜŞÜK, 07.10): sezgisel kural inceltildi —
#   · Contains ancak argümanı bir KİMLİK alanıysa (…Id ya da …Hash) "kimlik kümesi"dir. Metin araması
#     (c.Ad.Contains(q), tipler.Contains(x.Tur)) kimlik kümesi DEĞİLDİR → alttaki kurallara düşer.
#   · GroupBy anahtarı CariId/UrunId/FisId ise dönen satır cari/ürün/fiş sayısıyla BÜYÜR
#     → "BUYUYEN:toplama" (ayrı sayaç). Diğer GroupBy (gün, tür, maden…) DOĞAL:toplama kalır.
#   · Sıra: SAYFALI → kimlik kümesi → BÜYÜYEN toplama → doğal toplama → üst kayıt → yapılandırma → SINIRSIZ.
# ═════════════════════════════════════════════════════════════════════════════
use strict; use warnings;
my $say_kipi = 0;
if (@ARGV && $ARGV[0] eq '--say') { $say_kipi = 1; shift @ARGV; }
my %say;
my $YAPI = qr/\bdb\.(Subeler|Kasalar|Roller|Yetkiler|RolYetkileri|Ayarlar|UrunKategorileri|Reyonlar|GiderKategorileri|FiyatListeleri|BankaHesaplari|Bankalar|Kullanicilar|PanoAyarlari|Tercihler|Pariteler|KullaniciSubeleri|YasalParametreler|PosCihazlari|Kampanyalar|KiraciAyarlari)\b|Set<(Reyon|Sube|Kasa)>/;
my $KIMLIK_KUMESI = qr/\.Contains\(\s*(?:\w+\.)*\w*(?:Id|Hash)!?\s*\)/;
my $BUYUYEN_ANAHTAR = qr/\.GroupBy\(\s*\w+\s*=>\s*(?:new\s*\{[^}]*?)?\w+\.(?:CariId|UrunId|FisId)\b|\bgroup\s.+?\sby\s+(?:new\s*\{[^}]*?)?\w+\.(?:CariId|UrunId|FisId)\b/s;

sub sinif {
  my ($x) = @_;
  return 'SAYFALI'            if $x =~ /\.Take\(|\.Skip\(/;
  return 'DOGAL:kimlik'       if $x =~ $KIMLIK_KUMESI;
  return 'BUYUYEN:toplama'    if $x =~ $BUYUYEN_ANAHTAR;
  return 'DOGAL:toplama'      if $x =~ /\.GroupBy\(/ or $x =~ /\bgroup\s.+\sby\s/s;
  return 'DOGAL:ust-kayit'    if $x =~ /\.(?!TenantId\b)\w*Id\s*==\s*[\w.!]+/ or $x =~ /==\s*\w+\.(?!TenantId\b)\w*Id\b/;
  return 'DOGAL:yapilandirma' if $x =~ $YAPI;
  return 'SINIRSIZ';
}

for my $dosya (@ARGV) {
  next if $dosya =~ /\.mutorj$/;
  open my $f, '<:encoding(UTF-8)', $dosya or next; local $/; my $m = <$f>; close $f;
  # yorumları sil ama satır numarası korunsun diye satır sonlarını bırak
  $m =~ s{(?<![:"\\])//[^\n]*}{}g;
  my $poz = 0;
  for my $x (split /;/, $m) {
    my $bas = $poz; $poz += length($x) + 1;
    next unless $x =~ /\.ToListAsync\(\)|\.ToDictionaryAsync\(/;
    my $s = sinif($x);
    $say{$s}++;
    next if $say_kipi;
    my $satir = 1 + (() = substr($m, 0, $bas + length($x)) =~ /\n/g);
    (my $o = $x) =~ s/\s+/ /g; $o =~ s/^\s+//;
    (my $d = $dosya) =~ s{.*/Kuyumcu\.Api/}{};
    print join("\t", $d, $satir, $s, substr($o, -140)), "\n";
  }
}
if ($say_kipi) { print(($say{'SINIRSIZ'} // 0), ' ', ($say{'BUYUYEN:toplama'} // 0), "\n"); }
else { print STDERR join(' ', map { "$_=$say{$_}" } sort keys %say), "\n"; }
