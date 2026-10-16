#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
YEREL TLS KÖPRÜSÜ — inceleme sunucusu (5199) için düz-TCP ⇄ TLS köprüsü.

NEDEN VAR (2026-09-21, B-2 borcunun kapanışı — ÖLÇÜLMÜŞ GEREKÇE)
────────────────────────────────────────────────────────────────────────────
Kurucunun 5199 inceleme sunucusu ÜRETİM kipinde koşmak ZORUNDA (B-2): Swagger
kapalı, bekleyen migration kapısı açık, sır kara listesi etkin, geliştirme
ayar dosyası okunmuyor. Üretim kipi de TLS ister (UretimAcilisDenetimi,
fail-closed) — ve bu doğrudur, gevşetilmemelidir.

Ama yerel makinede tarayıcının GÜVENDİĞİ bir sertifika üretemiyoruz:
  • ASP.NET geliştirme sertifikası VAR (CN=localhost, 2027'ye kadar geçerli),
  • `dotnet dev-certs https --trust` YÖNETİCİ PAROLASI ister → kurucunun
    parolası kullanılmaz (kişisel bilgi) ve sistem güvenlik ayarı değiştirilmez,
  • uygulama içi tarayıcı `https://localhost:5201` sayfasını 2026-09-21'de
    AÇAMADI: sayfa yüklemesi başarısız (curl da "unable to get local issuer
    certificate" diyor; `curl -k` ile 200).

ÜÇ KÖTÜ YOL REDDEDİLDİ
  (a) Ortamı Development'a düşürmek → kurucu ürünü değil, geliştirme
      kısayollarını inceler; üstelik TÜM açılış kapısı devre dışı kalır.
      (21 Eylül'de bir süre böyle koştu; P-19 hasım doğrulayıcısı yakaladı.)
  (b) `Uretim:TersVekilArkasinda=true` demek → ortada ters vekil YOK; bu bir
      YALAN beyandır (Anayasa md.1).
  (c) Açılış kapısının TLS kuralını gevşetmek → güvenlik kapısı zayıflar.

BU YOL — HİÇBİR KAPI GEVŞEMEZ
  Kestrel GERÇEKTEN TLS konuşur: `https://localhost:5200`. Tarayıcının
  sertifikaya güvenmesi gerekmesin diye araya YALNIZ DÖNGÜ ARAYÜZÜNDE yaşayan
  bu köprü konur: 127.0.0.1:5199 düz TCP kabul eder, baytları TLS ile 5200'e
  taşır. Uygulama isteği https olarak görür (UseHttpsRedirection tetiklenmez),
  ters vekil beyanı yapılmaz, açılış kapısı tam güçte çalışır.

GÜVENLİK SINIRLARI (fail-closed, kod bunları uygular)
  • Dinleme YALNIZ 127.0.0.1 — dış arayüze bağlanmaz.
  • Hedef konak döngü arayüzünde değilse köprü AÇILMAZ (exit 2).
  • Bayt düzeyinde çalışır: HTTP/1.1, yığınlı gövde ve WebSocket yükseltmesi
    (SignalR) olduğu gibi geçer; ayrıştırma yok, dolayısıyla ayrıştırma hatası
    da yok. ALPN açıkça http/1.1'e sabitlenir.
  • Sertifika doğrulaması KAPALI (yerel geliştirme sertifikası kendinden
    imzalı) — bu köprü ağ üstünde YAŞAMADIĞI için ortadaki-adam tehdidi yok.
    Gerçek kurulumda bu köprü KULLANILMAZ; orada TLS uçtan uca gerçektir.

Kullanım: python3 yerel-tls-koprusu.py <dinleme-portu> <hedef-konak> <hedef-port>
"""
import socket
import ssl
import sys
import threading

DONGU_KONAKLARI = {"localhost", "127.0.0.1", "::1"}
TAMPON = 65536


def tasi(kaynak: socket.socket, hedef: socket.socket) -> None:
    """Tek yönde bayt taşır; taraflardan biri kapanınca yazma ucunu kapatır."""
    try:
        while True:
            veri = kaynak.recv(TAMPON)
            if not veri:
                break
            hedef.sendall(veri)
    except OSError:
        pass
    finally:
        try:
            hedef.shutdown(socket.SHUT_WR)
        except OSError:
            pass


def baglanti_isle(istemci: socket.socket, hedef_konak: str, hedef_port: int,
                  baglam: ssl.SSLContext) -> None:
    sunucu = None
    try:
        ham = socket.create_connection((hedef_konak, hedef_port), timeout=15)
        ham.settimeout(None)
        sunucu = baglam.wrap_socket(ham, server_hostname=hedef_konak)
        ileri = threading.Thread(target=tasi, args=(istemci, sunucu), daemon=True)
        ileri.start()
        tasi(sunucu, istemci)
        ileri.join()
    except OSError as hata:
        # Köprü tek bir bağlantı yüzünden ÖLMEZ; kurucunun ekranı ayakta kalır.
        print(f"köprü: bağlantı hatası — {hata}", flush=True)
    finally:
        for s in (istemci, sunucu):
            if s is not None:
                try:
                    s.close()
                except OSError:
                    pass


def main() -> int:
    if len(sys.argv) != 4:
        print(__doc__.strip().splitlines()[-1])
        return 2
    dinleme_portu = int(sys.argv[1])
    hedef_konak = sys.argv[2]
    hedef_port = int(sys.argv[3])

    # FAIL-CLOSED: köprü yalnız döngü arayüzü içindir.
    if hedef_konak not in DONGU_KONAKLARI:
        print(f"HATA: hedef konak '{hedef_konak}' döngü arayüzünde değil. "
              f"Bu köprü yalnız yerel inceleme sunucusu içindir — açılmıyor.")
        return 2

    baglam = ssl.SSLContext(ssl.PROTOCOL_TLS_CLIENT)
    baglam.check_hostname = False
    baglam.verify_mode = ssl.CERT_NONE          # yerel, kendinden imzalı sertifika
    try:
        baglam.set_alpn_protocols(["http/1.1"])  # HTTP/2 pazarlığı yapılmasın
    except NotImplementedError:
        pass

    dinleyici = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    dinleyici.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    dinleyici.bind(("127.0.0.1", dinleme_portu))   # YALNIZ döngü arayüzü
    dinleyici.listen(128)
    print(f"yerel TLS köprüsü: http://127.0.0.1:{dinleme_portu} "
          f"→ https://{hedef_konak}:{hedef_port} (yalnız döngü arayüzü)", flush=True)

    try:
        while True:
            istemci, _ = dinleyici.accept()
            istemci.settimeout(None)
            threading.Thread(target=baglanti_isle,
                             args=(istemci, hedef_konak, hedef_port, baglam),
                             daemon=True).start()
    except KeyboardInterrupt:
        return 0
    finally:
        dinleyici.close()


if __name__ == "__main__":
    sys.exit(main())
