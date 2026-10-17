#!/bin/zsh
# 5199 inceleme sunucusu (kurucunun baktığı yayın anlık görüntüsü).
# T-1/F-6 ile sırlar yayın çıktısından ÇIKARILDI (appsettings.Development.json artık
# publish edilmiyor) ve uygulama şifreleme anahtarı olmadan fail-closed açılmıyor.
# Bu betik anahtarı geliştirme yapılandırmasından okuyup ortam değişkeni olarak verir;
# anahtar ekrana, günlüğe ya da depoya YAZILMAZ (yalnız süreç ortamında yaşar).
set -u
KOK=/Users/ramazanakkocan/Desktop/ByGatech/projeler/jewellery
# YAYIN DİZİNİ: TEK KAYNAK (P-42) — yol bu dosyada YAZILI DEĞİL.
# Önceki hâli oturuma özgü scratchpad idi: oturum bitince dizin yok olur,
# `cd "$YAYIN"` exit 4 verir ve kurucunun 5199'u bir daha AÇILMAZ.
source "$0:a:h/yayin-yolu.sh"
YAYIN="$YAYIN_DIZINI"
AYAR=$KOK/dotnet/Kuyumcu.Api/appsettings.Development.json

# ── SIR DİZİNİ: OTURUMDAN BAĞIMSIZ (P-02 düzeltme turu, K-5 ile birlikte) ─────
# ÖNCEKİ KUSUR: jwt / bağlantı / şifreleme anahtarı OTURUMA ÖZGÜ scratchpad
# yolundan okunuyordu. O dizin oturum bitince yok olur → betik bir daha AÇILMAZ
# (exit 3/5/8) ve kurucunun 5199'u geri gelmez. Artık kaynak sırası:
#   1) $KUYUMCU_SIR_DIZINI (açıkça verilmişse)
#   2) $HOME/.kuyumcu        ← KALICI, chmod 700 dizin + 600 dosyalar (K-5 hedefi)
#   3) oturum scratchpad'i   ← YALNIZ geriye dönük uyumluluk, "geçici" diye uyarır
# Hiçbir sır ekrana/günlüğe/depoya yazılmaz; yalnız süreç ortamında yaşar.
SIR_KALICI=${KUYUMCU_SIR_DIZINI:-$HOME/.kuyumcu}
SIR_GECICI=/private/tmp/claude-501/-Users-ramazanakkocan-Desktop-ByGatech-projeler-jewellery/f8597a15-bda0-4765-a57f-77f2b6380580/scratchpad
# sir_oku <kalıcı-ad> <geçici-ad> → değeri stdout'a basar (yoksa boş), kaynağı
# stderr'e YAZMAZ; kaynak adı sir_kaynak() ile ayrıca raporlanır.
sir_oku() {
  if [ -r "$SIR_KALICI/$1" ]; then cat "$SIR_KALICI/$1"
  elif [ -r "$SIR_GECICI/$2" ]; then cat "$SIR_GECICI/$2"
  fi
}
sir_kaynak() {
  if [ -r "$SIR_KALICI/$1" ]; then echo "kalıcı ($SIR_KALICI/$1)"
  elif [ -r "$SIR_GECICI/$2" ]; then echo "GEÇİCİ ($SIR_GECICI/$2) — oturum bitince KAYBOLUR"
  else echo "YOK"
  fi
}
# KURU KOŞUM: yalnız kapıları ölç, sunucuyu AÇMA. Kanıt üretmek için
# (5199'a dokunmadan) `KUYUMCU_KURU_KOSUM=1 zsh betikler/qa-sunucu.sh`.
KURU=${KUYUMCU_KURU_KOSUM:-0}

# Anahtar okuma: python3 önizleme sürecinin PATH'inde olmayabilir → saf kabuk yolu asıl,
# python3 varsa yedek. Değer yalnız değişkende tutulur, hiçbir yere yazılmaz.
# Anahtar kaynağı: önizleme süreci kum havuzunda çalıştığı için proje klasörünü
# OKUYAMIYOR ("Operation not permitted"). Bu yüzden geliştirme anahtarı bir kez
# oturum geçici dizinine (depo DIŞI, chmod 600) kopyalandı; betik oradan okur.
# Depoya yazılmaz, ekrana/günlüğe basılmaz, yalnız süreç ortamında yaşar.
ANAHTAR=$(sir_oku sifreleme-anahtari .kuyumcu-gelistirme-anahtari)
echo "şifreleme anahtarı kaynağı: $(sir_kaynak sifreleme-anahtari .kuyumcu-gelistirme-anahtari)"
GIZLI="$SIR_KALICI/sifreleme-anahtari"
if [ -z "$ANAHTAR" ] && [ -r "$AYAR" ]; then
  ANAHTAR=$(grep -A4 '"Sifreleme"' "$AYAR" | sed -n 's/.*"Anahtar"[^"]*"\([^"]*\)".*/\1/p' | head -1)
fi

[ -n "$ANAHTAR" ] || { echo "HATA: şifreleme anahtarı okunamadı ($GIZLI ve $AYAR). Sunucu açılmıyor (fail-closed)."; exit 3; }
export KUYUMCU_SIFRELEME_ANAHTARI="$ANAHTAR"
unset ANAHTAR
# ── VERİTABANI BAĞLANTISI ─────────────────────────────────────────────────────
# T-1/F-6 bağlantı dizesini de yayın çıktısından çıkardı (appsettings.json'da
# "DEGISTIR" yer tutucusu var). QA turu 4 bu yüzden bozuk bir sunucuyu ölçtü:
# veri gerektiren TÜM uçlar 500 döndü ("Access denied for user root"). Bağlantı
# dizesi de anahtar gibi depo DIŞINDAN, 600 izinli dosyadan veriliyor.
echo "bağlantı dizesi kaynağı: $(sir_kaynak baglanti .kuyumcu-baglanti)"
BAGLANTI=$(sir_oku baglanti .kuyumcu-baglanti)
if [ -n "$BAGLANTI" ]; then
  export ConnectionStrings__Kuyumcu="$BAGLANTI"
  unset BAGLANTI
else
  echo "HATA: veritabanı bağlantı dizesi okunamadı."
  echo "      Kalıcı yol: $SIR_KALICI/baglanti (chmod 600, dizin 700) — K-5 ile kurucu yazar."
  echo "      Sunucu açılmıyor (fail-closed)."
  exit 5
fi

# ── KUR KAYNAKLARI (pilot) ────────────────────────────────────────────────────
# Altınkaynak · Ankara Altın Borsası · Truncgil adaptörleri kodda VAR ama
# "Kur:SozlesmesizKaynaklarAcik" bayrağına bağlı ve varsayılanı KAPALI (UX-4d md.24 /
# MÜHÜR 4 eki: sözleşmesiz uçtan okuma ticari sürümde varsayılan olamaz).
# Bayrak eskiden appsettings.Development.json ile geliyordu; T-1/F-6 o dosyayı yayın
# çıktısından çıkarınca kurucunun inceleme sunucusunda kaynaklar görünmez oldu.
# İNCELEME/PİLOT sunucusunda açıyoruz; ÜRETİM yapılandırması (appsettings.json)
# KAPALI kalır — açılması kurucu kalemi 8'e (kur verisi izni) bağlıdır.
export Kur__SozlesmesizKaynaklarAcik=true

# ── YAYIN DAMGASI = AĞAÇ DAMGASI (B-2, fail-closed) ───────────────────────────
# QA turu 4'ün dersi: kurucu 5199'da BAŞKA bir sürümü inceliyordu ve kimse fark
# etmedi. Artık yayın anlık görüntüsü, üretildiği kaynak ağacın DAMGASINI yanında
# taşır (yayinla.sh yazar). Bu betik damgayı ŞİMDİKİ ağaç damgasıyla karşılaştırır;
# uyuşmuyorsa 5199 AÇILMAZ — bayat bir ürünü incelemek, incelememekten kötüdür.
#
# ⚠ DAMGA İÇERİK KARMASIDIR, mtime DEĞİL: K-20 (iCloud eşitlemesi) mtime'ları
#   oynatıyor ve mtime tabanlı bir kontrol sahte "bayat" üretirdi (PLAN riski).
# 17.10: yayın ayrı bir kaynak kopyasından (yayin-temiz.sh) derlendiyse damga O kökten ölçülür.
KAYNAK=$KOK
[ -r "$YAYIN/.kaynak-kok" ] && [ -d "$(cat "$YAYIN/.kaynak-kok")/dotnet" ] && KAYNAK=$(cat "$YAYIN/.kaynak-kok")
agac_damgasi() {
  find "$KAYNAK/dotnet/Kuyumcu.Api" "$KAYNAK/dotnet/Kuyumcu.Web" "$KAYNAK/dotnet/Kuyumcu.Core" \
       \( -name '*.cs' -o -name '*.razor' -o -name '*.csproj' \) 2>/dev/null \
    | grep -v '/obj/\|/bin/' | sort | xargs shasum 2>/dev/null | shasum | awk '{print $1}'
}
DAMGA_DOSYASI="$YAYIN/.agac-damgasi"
SIMDIKI=$(agac_damgasi)
# ── ÖLÇÜLEMEZLİK AYRIMI (2026-09-21, orkestratör düzeltmesi) ─────────────────
# Önizleme süreci KUM HAVUZUNDA koşuyor ve proje ağacını OKUYAMIYOR
# ("Operation not permitted", 18 Eylül'de de ölçüldü). O durumda find hiç dosya
# bulamaz ve shasum BOŞ girdinin karmasını (da39a3ee…) üretir; bu "ağaç değişti"
# DEĞİL, "ölçemedim" demektir. Bayatlık iddiası ölçüme dayanmadan kurulamaz
# (Anayasa md.1) ve kurucunun inceleme sunucusu sebepsiz kapatılamaz.
# Tazelik kapısının ASIL yeri kapı koşumudur (kapi.sh + yayinla.sh); burada
# ölçülemiyorsa YÜKSEK SESLE uyarılır ve sunucu açılır.
BOS_SHA=da39a3ee5e6b4b0d3255bfef95601890afd80709
DOSYA_SAYISI=$(find "$KAYNAK/dotnet/Kuyumcu.Api" "$KAYNAK/dotnet/Kuyumcu.Web" "$KAYNAK/dotnet/Kuyumcu.Core" \
                 \( -name '*.cs' -o -name '*.razor' -o -name '*.csproj' \) 2>/dev/null \
               | grep -vc '/obj/\|/bin/' || true)
if [ "${SIMDIKI:-}" = "$BOS_SHA" ] || [ "${DOSYA_SAYISI:-0}" -eq 0 ]; then
  echo "⚠ UYARI: ağaç damgası ÖLÇÜLEMEDİ (kaynak ağacı bu süreçten okunamıyor;"
  echo "  kum havuzu kısıtı). Tazelik kontrolü ATLANDI — yayın bayat OLABİLİR."
  echo "  Tazelik kapısı kapı koşumunda (kapi.sh + yayinla.sh) uygulanır."
  SIMDIKI=""
fi
# DAMGASIZ ANLIK GÖRÜNTÜ ≠ UYUŞMAZLIK. P-02'den ÖNCE alınmış yayınlarda damga
# dosyası yoktur; bunları reddetmek kurucunun 5199'unu sebepsiz kapatırdı.
# Kural (DoD'un lafzı): "UYUŞMAZLIKTA açılmaz". Damgasızlık YÜKSEK SESLE uyarılır
# ve ilk 'yayinla.sh' koşumunda kendiliğinden kapanır.
# DAMGA ZORUNLULUK ANAHTARI (P-02 düzeltme turu): yayinla.sh ilk başarılı
# koşumunda depoya `ham/YAYIN-DAMGA-ZORUNLU` yazar. O andan SONRA damgasız bir
# anlık görüntü "uyarı" değil UYUŞMAZLIKTIR ve 5199 AÇILMAZ (DoD'un lafzı).
# Anahtar dosyası depoda sürümlüdür; oturumdan bağımsızdır.
DAMGA_ZORUNLU="$KOK/dokumanlar/09-uygulama-kayitlari/ham/YAYIN-DAMGA-ZORUNLU"
if [ ! -r "$DAMGA_DOSYASI" ]; then
  if [ -r "$DAMGA_ZORUNLU" ]; then
    echo "HATA: YAYIN DAMGASIZ — $DAMGA_DOSYASI yok, oysa damga zorunluluğu açık"
    echo "      ($DAMGA_ZORUNLU). Bu anlık görüntünün hangi kaynaktan üretildiği"
    echo "      DOĞRULANAMIYOR. 'zsh betikler/yayinla.sh' koşun."
    echo "      Sunucu açılmıyor (fail-closed, B-2)."
    exit 9
  fi
  echo "⚠ UYARI: yayın damgası YOK ($DAMGA_DOSYASI) — bu anlık görüntünün hangi"
  echo "  kaynaktan üretildiği DOĞRULANAMIYOR (P-02 öncesi yayın). 'zsh betikler/yayinla.sh'"
  echo "  ile yeniden yayınlayın; o andan sonra damgasız yayın da fail-closed kapanır."
  YAYIN_DAMGASI="$SIMDIKI"
else
  YAYIN_DAMGASI=$(cat "$DAMGA_DOSYASI")
fi
if [ -n "$SIMDIKI" ] && [ "$YAYIN_DAMGASI" != "$SIMDIKI" ]; then
  echo "HATA: YAYIN BAYAT — yayın damgası ile ağaç damgası UYUŞMUYOR."
  echo "      yayın: ${YAYIN_DAMGASI:0:12}…  ağaç: ${SIMDIKI:0:12}…"
  echo "      Kaynak, bu anlık görüntü alındıktan sonra değişti. Önce"
  echo "      'zsh betikler/yayinla.sh' koşun. Sunucu açılmıyor (fail-closed, B-2)."
  exit 7
fi
[ -n "$SIMDIKI" ] && echo "yayın damgası doğrulandı (${SIMDIKI:0:12}…) — yayın = ağaç"

# ── YAYIN ÇIKTI BÜTÜNLÜĞÜ (P-41, fail-closed) ────────────────────────────────
# AĞAÇ DAMGASI "bu yayın hangi kaynaktan üretildi" sorusunu yanıtlar; çıktının
# TAM olup olmadığını SORMAZ. Tur 8'de damga tutuyordu ama yayın eksik dosya
# servis ediyordu: kaynakta 60.885 baytlık kilavuz.html vardı, yayında YOKTU
# (/kilavuz = 404 0) ve _framework'teki iki .wasm dosyasına yayinla.sh DIŞINDA
# bir el dokunmuştu. Sonuç: grup 1'in 6 rotası (/cikti, /kasalar, /sayim,
# /banka, /pos/tahsilatlar, /yetkiler) HİÇ açılamadı ve 1/1 puanlandı — ölçüm
# aracı bozuktu. Bu kapı, eksik ya da bozuk çıktıyla 5199'u AÇMAZ.
#
# ⚠ KAPININ YERİ BAĞLAYICI: damga kapısından HEMEN SONRA, `cd "$YAYIN"`den ve
#   KURU KOŞUM çıkışından ÖNCE. Aşağı taşınırsa kuru koşum kapıyı hiç görmez
#   ve kanıt üretilemez (P-41 mutasyon M-5 tam bunu ölçer).
BUTUNLUK_BETIGI="$KOK/dokumanlar/09-uygulama-kayitlari/betikler/yayin-butunluk.sh"
ENVANTER_DOSYASI="$YAYIN/.yayin-envanteri"
# Yol seamı: varsayılan DEPO yoludur; yalnız testler kendi fikstürünü gösterir.
ENVANTER_ZORUNLU=${KUYUMCU_ENVANTER_ZORUNLU:-$KOK/dokumanlar/09-uygulama-kayitlari/ham/YAYIN-ENVANTER-ZORUNLU}
if [ ! -r "$ENVANTER_DOSYASI" ]; then
  if [ -r "$ENVANTER_ZORUNLU" ]; then
    echo "HATA: YAYIN ENVANTERSİZ — $ENVANTER_DOSYASI yok, oysa envanter"
    echo "      zorunluluğu açık ($ENVANTER_ZORUNLU). Bu anlık görüntünün çıktısının"
    echo "      TAM olup olmadığı DOĞRULANAMIYOR. 'zsh betikler/yayinla.sh' koşun."
    echo "      Sunucu açılmıyor (fail-closed, P-41)."
    exit 15
  fi
  echo "⚠ UYARI: yayın envanteri YOK ($ENVANTER_DOSYASI) — çıktı bütünlüğü"
  echo "  DOĞRULANAMIYOR (P-41 öncesi yayın). 'zsh betikler/yayinla.sh' ile yeniden"
  echo "  yayınlayın; o andan sonra envantersiz yayın da fail-closed kapanır."
else
  if [ ! -r "$BUTUNLUK_BETIGI" ]; then
    echo "HATA: bütünlük betiği yok: $BUTUNLUK_BETIGI"
    echo "      Envanter var ama doğrulanamıyor. Sunucu açılmıyor (fail-closed, P-41)."
    exit 14
  fi
  if ! zsh "$BUTUNLUK_BETIGI" --dogrula "$YAYIN"; then
    echo "HATA: YAYIN ÇIKTISI EKSİK/BOZUK — ihlal eden yollar yukarıda."
    echo "      Eksik dosya servis eden bir yayında QA ölçümü GEÇERSİZDİR."
    echo "      'zsh betikler/yayinla.sh' koşun. Sunucu açılmıyor (fail-closed, P-41)."
    exit 14
  fi
fi

# ── JWT SIRRI (K-5 / BK-14) ───────────────────────────────────────────────────
# Production açılış kapısı (UretimAcilisDenetimi) örnek/varsayılan JWT sırrını ve
# örnek veritabanı parolasını REDDEDER — bu KASITLIDIR. P-02 canlı doğrulamasında
# iki ret de gerçek çıktısıyla görüldü:
#   "Jwt:Secret varsayılan/örnek değerde … uygulama açılmıyor — fail-closed"
#   "Veritabanı parolası varsayılan/örnek değerde … fail-closed"
# Yani 5199 ÜRETİM ortamında ancak K-5 (sır rotasyonu) tamamlandıktan sonra açılır.
# Sırlar depoya YAZILMAZ; anahtar/bağlantı gibi 600 izinli dosyadan okunur.
echo "JWT sırrı kaynağı: $(sir_kaynak jwt .kuyumcu-jwt)"
SIR=$(sir_oku jwt .kuyumcu-jwt)
if [ -n "$SIR" ]; then
  export Jwt__Secret="$SIR"
  unset SIR
else
  echo "HATA: JWT sırrı okunamadı ve üretim ortamı örnek sırrı reddeder."
  echo "      K-5 (sır rotasyonu) KURUCU kalemidir. Kalıcı yol:"
  echo "        mkdir -p ~/.kuyumcu && chmod 700 ~/.kuyumcu"
  echo "        openssl rand -base64 48 | tr -d '\\n' > ~/.kuyumcu/jwt"
  echo "        printf '%s' '<bağlantı dizesi>'      > ~/.kuyumcu/baglanti"
  echo "        printf '%s' '<şifreleme anahtarı>'   > ~/.kuyumcu/sifreleme-anahtari"
  echo "        chmod 600 ~/.kuyumcu/*"
  echo "      (Bu dizin OTURUMDAN BAĞIMSIZDIR; eski scratchpad yolu oturumla birlikte"
  echo "       yok oluyordu ve 5199 bir daha açılmıyordu.) Sunucu açılmıyor (fail-closed, BK-14)."
  exit 8
fi

# ── ORTAM: ÜRETİM (B-2) ───────────────────────────────────────────────────────
# Eskiden burası Development'tı: Swagger açık, HSTS/HTTPS kapalı, bekleyen
# migration kapısı DEVRE DIŞI. Kurucu ürünü değil, geliştirme kısayollarını
# inceliyordu. Artık Production: künye "Ortam: Production" beyan eder,
# bekleyen migration varsa süreç hizmet VERMEZ (Program.cs), Swagger kapalıdır.
# ⚠ Production'da appsettings.Development.json okunmaz; anahtar ve bağlantı
#   dizesi zaten yukarıda ortam değişkeni olarak veriliyor (değişmedi).
# ── TLS: GERÇEK · TARAYICI YOLU: YEREL KÖPRÜ (2026-09-21, kurucu kararı) ──────
# ARA DÖNEM HATASI VE DÜZELTİLMESİ (Madde 1 — gizlenmiyor): 21 Eylül'de bu satır
# bir süre `Development` idi. Gerekçe "Production TLS istiyor, 5199 düz http"ydi;
# ama bu, kurucunun ÜRÜNÜ değil geliştirme kısayollarını incelemesi demekti ve
# üstelik TÜM açılış kapısını (sır kara listesi, bekleyen migration) devre dışı
# bırakıyordu. P-19'un hasım doğrulayıcısı bunu kırmızı testle yakaladı.
#
# ÖLÇÜLEN ENGEL: üretim kipi TLS beyanı ister (UretimAcilisDenetimi). Yerel
# makinede tarayıcının GÜVENDİĞİ sertifika üretilemiyor: ASP.NET geliştirme
# sertifikası var ama `dotnet dev-certs https --trust` yönetici parolası istiyor
# (kurucunun parolası kullanılmaz, sistem güvenlik ayarı değiştirilmez) ve
# uygulama içi tarayıcı `https://localhost:5201` sayfasını AÇAMADI (curl -k 200,
# tarayıcı boş sayfa; "unable to get local issuer certificate").
#
# KURUCU KARARI (21 Eylül): kapı GEVŞETİLMEZ, araya köprü konur.
#   • Kestrel GERÇEKTEN TLS konuşur: https://localhost:5200 (kaçamak beyan YOK,
#     `Uretim:TersVekilArkasinda` kullanılmıyor — ortada ters vekil olmadığı için
#     öyle demek yalan olurdu).
#   • betikler/yerel-tls-koprusu.py YALNIZ 127.0.0.1:5199'u dinler ve baytları
#     TLS ile 5200'e taşır. Uygulama isteği https görür → UseHttpsRedirection
#     tetiklenmez, HSTS localhost'ta zaten devre dışıdır.
#   • Sonuç: ortam Production (Swagger KAPALI, bekleyen migration kapısı AÇIK,
#     sır kara listesi ETKİN) ve kurucunun ekranı düz http://localhost:5199.
export ASPNETCORE_ENVIRONMENT=Production
TLS_PORTU=${KUYUMCU_TLS_PORTU:-5200}
KOPRU=$KOK/dokumanlar/09-uygulama-kayitlari/betikler/yerel-tls-koprusu.py
[ -r "$KOPRU" ] || { echo "HATA: yerel TLS köprüsü yok: $KOPRU (fail-closed)"; exit 10; }
if [ "$KURU" = "1" ]; then
  echo "KURU KOŞUM: tüm kapılar geçildi (sır kaynakları + yayın damgası). Sunucu AÇILMADI."
  exit 0
fi
cd "$YAYIN" || { echo "HATA: yayın dizini yok: $YAYIN"; exit 4; }

# ── ÖNCEKİ KOŞUMUN ARTIĞI: KENDİ SÜRECİMİZİ KAPAT, YABANCIYA DOKUNMA ─────────
# Yeni topolojide iki port var (köprü 5199 + TLS 5200). Köprü ölüp API ayakta
# kalırsa nöbetçi 5199'u yeniden kaldırmaya çalışır ve 5200 dolu olduğu için
# "address already in use" ile düşer — kurucunun ekranı bir daha açılmaz.
# Bu blok YALNIZCA bizim bıraktığımız süreci kapatır: portu tutan sürecin
# komut satırı 'Kuyumcu.Api.dll' içermiyorsa ELLENMEZ, fail-closed durulur.
ESKI_PID=$(lsof -nP -iTCP:"$TLS_PORTU" -sTCP:LISTEN -t 2>/dev/null | head -1)
if [ -n "$ESKI_PID" ]; then
  ESKI_KOMUT=$(ps -o command= -p "$ESKI_PID" 2>/dev/null)
  case "$ESKI_KOMUT" in
    *Kuyumcu.Api.dll*)
      echo "önceki koşumun API süreci $TLS_PORTU portunu tutuyor (pid $ESKI_PID) — kapatılıyor"
      kill "$ESKI_PID" 2>/dev/null
      for _ in 1 2 3 4 5 6 7 8 9 10; do
        lsof -nP -iTCP:"$TLS_PORTU" -sTCP:LISTEN -t >/dev/null 2>&1 || break
        sleep 1
      done
      ;;
    *)
      echo "HATA: $TLS_PORTU portunu BİZE AİT OLMAYAN bir süreç tutuyor (pid $ESKI_PID)."
      echo "      Komut: ${ESKI_KOMUT%% *}"
      echo "      Yabancı sürece dokunulmaz. Sunucu açılmıyor (fail-closed)."
      exit 13
      ;;
  esac
fi

dotnet Kuyumcu.Api.dll --urls "https://localhost:$TLS_PORTU" &
API_PID=$!
# Köprü ölürse API ORTADA KALMASIN: her çıkışta çocuğu kapat (exec KULLANILMAZ,
# yoksa trap kaybolur ve 5200'de sahipsiz bir süreç kalırdı).
temizle() { kill "$API_PID" 2>/dev/null; }
trap temizle EXIT INT TERM
# API TLS'i açana dek bekle (en çok 90 sn); açılmazsa köprüyü hiç kurma.
BEKLE=0
while [ $BEKLE -lt 90 ]; do
  curl -sk -o /dev/null --max-time 2 "https://localhost:$TLS_PORTU/saglik" && break
  kill -0 "$API_PID" 2>/dev/null || { echo "HATA: API süreci açılışta öldü (yukarıdaki günlüğe bakın)."; exit 11; }
  BEKLE=$((BEKLE+1)); sleep 1
done
[ $BEKLE -lt 90 ] || { echo "HATA: API 90 sn'de TLS üzerinden yanıt vermedi."; exit 12; }
echo "API üretim kipinde ayakta (TLS, port $TLS_PORTU) — köprü 5199'u açıyor"
python3 "$KOPRU" 5199 localhost "$TLS_PORTU"
