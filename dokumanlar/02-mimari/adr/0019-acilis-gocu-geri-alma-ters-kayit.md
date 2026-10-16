# ADR-0019 — Açılış göçü (Faz B) STORNO ile değil, TERS KAYITLA geri alınır

- **Tarih:** 2026-10-01
- **Durum:** KABUL EDİLDİ (GOC-47B uygulaması; hasım doğrulayıcı bulgusu üzerine yazıldı)
- **Bağlam:** PAKET-47 Faz B (açılış bakiyesi + açılış stoku / DEVIR partisi) · T-2 kurulum sihirbazı açılışı · 48-A storno motoru
- **Not:** Şartname bu kaydı "ADR-0016 (veri içe aktarımı)" olarak anıyordu; 0016 numarası reyon yerleşimine verilmiş olduğundan bu kayıt **0019** numarasıyla açıldı.

## Bağlam

Şartname Faz B için "geri alma storno ile" diyor. 48-A storno motorunun
`StornolanabilirTurler` listesi `ACILIS` ve `SAYIM_DUZELTME` türlerini **bilinçli olarak
içermez** (409). Bu türleri listeye eklemek, kurulum mühürlendikten sonra da genel
storno ekranından açılış kayıtlarının bozulmasına yol açardı — mühür (T-2) arka kapısı.

## Karar

1. Açılış göçünün (CSV, `acilis-bakiye` / `acilis-stok`) ve sihirbaz açılışının
   (T-2 `AcilisServisi.GeriAl`) geri alınması **storno istisnasıdır**: 48-A motoru
   kullanılmaz; aynı tek yazıcıdan (cari: `CariServisi` → `CariBakiyeYazici`;
   stok: `Kurulum/AcilisYazici`) **ters kayıt** yazılır. Hiçbir satır silinmez (ADR-0004).
2. Ters kayıt yalnız **kurulum mühürlenmeden** ve açılış penceresi açıkken yazılabilir;
   mühürden sonra 409. Mühürden sonraki düzeltme yolu olağan düzeltme fişi / sayımdır.
3. DEVIR partisinden satış/düşüm yapılmışsa ters kayıt **hiçbir kalemde** yazılmaz (409;
   kısmi geri alma yok).
4. **Adet işareti:** `hareketler.adet` sözleşme gereği İŞARETSİZDİR (≥ 0; `Data/Entities.cs`
   `Hareket.Adet` belgesi; kapı `HareketFabrikasi.Ekle`). Yön `has_grami` / `tl_tutari`
   kolonlarındadır. Ters `SAYIM_DUZELTME` satırında `adet` POZİTİF, `has_grami` NEGATİF
   yazılır — bu, satış/iade satırlarıyla aynı sözleşmedir; adedi de eksi yazmak aynı yönü
   iki kez saymak olurdu.

## Sonuçlar

- Ekstrede açılış ve ters satırı birlikte görünür (net 0); ayna kuralı `ACILIS`'i
  `CARI_DUZELTME` ile aynı sayar.
- Storno motoru ACILIS/SAYIM_DUZELTME için 409 vermeye devam eder (değişmedi).
- Testler: `Goc47BTestleri.Geri_al_TERS_KAYITLA_bakiye_ve_stok_onceki_haline_doner_kayit_silinmez`,
  `Acilis_stogu_tuketilmisse_geri_al_REDDEDILIR_kismi_geri_alma_YOK`,
  `Muhurlu_kurulumda_acilis_icee_aktarimi_409_arka_kapi_YOK`.
