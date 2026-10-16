# Tam tur aracı (kurucu 25.09: "her yeri istisnasız gez")
Başsız Chrome + ham CDP; tarayıcı bölmesi gizliyken de çalışır. Oturum `inceleme-vekili.py` ile parolasız açılır.

    cd betikler/tur
    # vekil: python3 ../inceleme-vekili.py <fikstur.json> <api_port> <vekil_port>
    TUR_HEDEF=http://localhost:<vekil_port> TUR_OUT=<cikti_dizini> node tur.mjs <isci_no> <BASIT|PRO> <aydinlik|karanlik> <1440,1280,1024,960,768,375> </rota1,/rota2>
    python3 analiz.py <cikti_dizini>

Ölçer: sikisma-olc K1..K11 + K12 ezik kutu · K13 kırpılan (kaydırılabilir kap içi hariç) · K10b kesik değer · K9b etiket binmesi;
her sekme ([role=tab]) tıklanır; her güvenli açıcı tıklanır → katman türü (bagli-acilir / cekmece / tam-ekran / SERBEST-YUZEN),
ekran dışı, sayfa kayması, yazı boyu (<14 px = kucuk-yazi), iç taşma. Kayıt/silme/gönderme düğmelerine basılmaz.
Mod kullanıcıya sunucuda yazılır — aynı kullanıcıyla aynı anda farklı modda iki işçi KOŞMA. Giriş sınırı 5/dk: işçileri ≥15 sn arayla başlat.

**Not (25.09 12:45):** TUR_OUT mutlak yol kabul eder; tarayıcı profili TUR_OUT (ya da TUR_PROFIL) altına yazılır — proje ağacına ASLA. CDP portu TUR_CDP ile seçilir.
