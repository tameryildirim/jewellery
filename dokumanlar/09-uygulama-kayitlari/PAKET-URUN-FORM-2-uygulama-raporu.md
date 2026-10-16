# PAKET URUN-FORM-2 uygulama raporu (hasimsiz paket)

## Yapilanlar
1. ORTAK kaydirma (ModalPencere gövdesi): `wwwroot/js/cekmece.js alanaOdakla` modal icinde scrollIntoView yerine yalniz `.modal-govde` kaydirir
   (focus preventScroll, panel.scrollTop=0, alan kabugunun ALT kenari + 12 px pay; hata satiri sonradan cizildigi icin 2 kare + 150 ms sonra tekrar olcer).
   `app.css`: `.modal-govde { scroll-padding-bottom }`. Tum pencereler kazanir.
2. Ret aninda aciklama ipucu gizli: `.modal-panel .alan-hatali .alan-ipucu { display:none }` (app.css).
3. Alana bagli retde bant kisa genel cumle: `Urunler.razor` UrunOlustur catch -> `FormDurumu.BantMesaji(...)` esleniyorsa `Dil.T.Urun.FormAlanHatasiGenel`
   ("Kayit yapilmadi. Kirmizi isaretli alani duzeltin." / EN); alan disi hata bantta TAM kalir.
4. Adet mesaji: `UrunModulu.cs` Range -> `AcilisAdediAttribute` (UrunAcilisSozlugu.Tr tek kaynak): `<0,001` -> "Adet sifirdan buyuk olmali.",
   `>1.000.000` -> "Adet cok buyuk — en fazla 1.000.000 girilebilir." (TR+EN sozluk anahtarlari).
## Sapma
- Eski E1 testi (UrunAcilisAtomikTestleri) bantta tam cumle bekliyordu; yeni sozlesmeye gore guncellendi (bant tam cumleyi ICERMEZ).
- Range yerine ozel nitelik: denetim.sh DTO_DECIMAL_RANGESIZ sayaci ilk denemede 21 (tavan 20) verdi; `// Range(...)` aciklama satiri ile 20'ye iner (nitelik adi sayac kalibina uymuyor, kural fiilen var).
## Migration: gerekmedi.
## Testler (yeni 12): UrunForm2Testleri.cs (F1, F2 x2, F2b, F3, F6) + UrunForm2EkranTestleri (F4 x2 BASIT/PRO, F5).
## Mutasyon (hepsi cmp AYNI)
M1 ust sinir x1000 -> F1 kirmizi · M2 mesaj degisimi -> F2 (adet 0, -5) · M3 bant eski haline -> F4 BASIT/PRO + E1 BASIT/PRO ·
M4 focus preventScroll kaldir -> F6 (ilk denemede KIRMIZI OLMADI = test acigi; test `e.focus({ preventScroll: true });` tam satiriyla guclendirildi, tekrar kirmizi) · M5 CSS ipucu kurali kaldir -> F6.
## Canli (5166, test kiraci PAKETURUN-FORM-2-TEST, silindi)
adet 999999999 -> 400 "Adet cok buyuk — en fazla 1.000.000 girilebilir." (alanlar acilisStogu.adet; SQL urun=0) · adet 0 ve -5 -> 400 "Adet sifirdan buyuk olmali." (urun=0) · adet 1.000.000 -> 201 · adet 5 -> 201 (urun=1).
## Gercek tarayici (basliksiz Chrome, sunucu reddi gram 5000 x 1.000.000): hata satiri alt kenari (ahAlt) <= alt cubuk ust kenari (barUst), 12/12 kombinasyon
1440/1280/375 x BASIT/PRO x acik/koyu: 1440 BASIT 720<=733, PRO 721<=733 · 1280 625<=638 / 626<=638 · 375 627<=649 / 637<=649; acik=koyu ayni.
Ilk denemede PRO 3 genislikte KALDI (770>733 vb.), cizim sonrasi yeniden olcum eklenince gecti. Ipucu gorunur=false; bant = kisa genel cumle; 'adet' senaryosu (999999999) 1280/375: alan altinda "Adet cok buyuk — en fazla 1.000.000 girilebilir.".
sikisma-olc: K1 0, K3 0/0, K5 0, K10 0, K11 0, yatay 0 (K9 6 = onceden var olan etiket-binme olcumu, bu paketle ilgisiz). Goruntuler: dokumanlar/09-uygulama-kayitlari/ham/URUN-FORM-2-olcum/*.png (PRO 1440 acik ve PRO 375 koyu Read ile gorundu); sayisal kayit ui3.jsonl.
## Kapi
build 0 hata (0 uyari bu pakette; 4 uyari baska seritlerin test dosyalarinda) · tam suit 11169 test, 1 kirmizi: SATIS3EkranTestleri.Barkod_dizisi_ve_Enter_DOGRUDAN_ekler_bekleyen_arama_IPTAL (benim degil; yeniden kosuda gecti = kararsiz) → YENI KIRMIZI 0.
denetim.sh: benim sayacim DUZELDI; kalan tek ihlal P-30 DEMO_INSAN_SURESI (demo-prova json, orkestrator sahibi, bu paketle ilgisiz). Son yapi sonrasi 191 ilgili test yesil.
## Dokunulan sayfalar: Urunler.razor (yalniz yeni urun penceresi hata gosterimi).
## Degisen dosyalar: Kuyumcu.Api/Modules/Urun/UrunModulu.cs · Kuyumcu.Core/I18n/Sozlukler.cs, Sozlukler.UrunAcilis.cs · Kuyumcu.Web/Pages/Urunler.razor, wwwroot/js/cekmece.js, wwwroot/css/app.css · Kuyumcu.Tests/UrunForm2Testleri.cs (yeni), UrunAcilisAtomikTestleri.cs (E1).
## Kurucu/mali musavir notu: yok (mevzuat/para/veri davranisina dokunulmadi).
