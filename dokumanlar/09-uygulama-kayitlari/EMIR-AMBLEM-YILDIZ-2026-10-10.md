# EMİR — Mihenk amblemi: üç boyutlu taş + MİHENK yazısı (Yıldız teması)

**Kimden:** Kurucu Ramazan Akkocan · **Tarih:** 10 Ekim 2026
**Kime:** Ana oturum (Tamer Bey → Ertuğ/Frontend, Aşkın/QA)
**Öncelik:** Yıldız temasının parçası; tema paketiyle birlikte, **eksiksiz ve aynen** uygulanır. Yorum, sadeleştirme, "benzeri" yok.

## 1. Kaynak (tek doğru)
Tuval: https://claude.ai/artifact/2eYLdsE7qXXZyGAaJZVGVo
- `project/Amblem_sol.dc.html` — kabuk, sol üst, koyu (52 px)
- `project/Amblem_sol_acik.dc.html` — kabuk, sol üst, açık
- `project/Giris4.dc.html`, `project/Giris4a.dc.html` — giriş ekranı, ortada (96 px)
Bu dosyalardaki `<span class="amb-parla">…</span>` bloğu ve ilgili CSS **olduğu gibi** alınır. Kurucu bu dosyaları tarayıcı görüntüsüyle onayladı (10 Ekim).

## 2. Amblem nedir
Mihenk amblemi bir **pırlanta taş**tır: 8 yüzlü (4 taç + 4 pavyon), üst ve alt yarısı eşit, ekvator tam ortada. Önden bakınca çizim şudur ve bu çizim **değişmez**: dış elmas silueti, ortadan geçen yatay çizgi, tepeden ve dipten yatay çizgiye inen iki iç kesim çizgisi. Bütün çizgiler eşit kalınlıkta, titanyum gri; üstünden ışık geçer.

## 3. Ölçüler (S = kutu boyu: **kabukta 52 px**, girişte 96 px; 40 px denendi, iç kesim çizgileri karıştı, kurucu reddetti)
- Ekvator yarıçapı r = 0,44·S · taç ve pavyon yüksekliği h = 0,50·S (toplam yükseklik S)
- Yüz: eşkenar olmayan üçgen; taban e = r·√2, yükseklik s = √(h² + (r/√2)²)
- Yüzlerin yerleşimi: `rotateY(45°+90°·k) translateZ(r/√2) rotateX(±atan((r/√2)/h))` — taç +, pavyon −; taban kenarı (`Z`) **taç yüzlerde çizilir** (yatay çizgi buradan gelir)
- Çizgi: kabukta 1,0 px, girişte 2,2 px; `stroke="url(#ti)"`, `stroke-linejoin:round`
- Perspektif: `perspective: 16·S` (az; çizim gibi düz dursun)
- Duruş açısı: **20°** (`rotateY(20deg)`); iç kesim çizgileri bu açıda çizimdeki yerinde görünür

## 4. Hareket (saniyeler değiştirilmez)
1. **Dönüş:** taş düşey ekseninde, üst ve alt uç sabit, **28 sn'de bir tam tur**, sabit hız (`rotateY 20° → 380°`, linear, sonsuz). Her 90°'de yine çizimdeki görünüme gelir.
2. **Işık:** titanyum gradyan (`#6A6A6A → #9A9A9A → #FFFFFF → #9A9A9A → #6A6A6A`) **11 sn'de bir** soldan sağa tek geçiş (SMIL `animateTransform` gradientTransform, `values="-1.2 0;-1.2 0;1.2 0;1.2 0" keyTimes="0;0.06;0.22;1"`). Taş ve yazı aynı gradyanı paylaşır.
3. **Parlama:** 11 sn döngüsünün %18'inde zirve `drop-shadow(0 0 7px var(--glow))`, sonra söner. Süzgeç **taşın dış sarmalayıcısında** (`.amb-parla`) durur; 3B kutunun kendisine konursa tarayıcı 3B'yi düzleştirir (yaşandı).
4. **Yazı:** `MİHENK`, Inter 400, büyük harf, harf aralığı 4,4 px (kabukta 13 px, girişte 26 px/ortalı). **Bütün olarak** karanlıktan çıkar: 11 sn döngüde `0–12 % görünmez+4 px bulanık → 24 % net → 82 % net → 91 % karanlık+3 px bulanık`. Harf harf DEĞİL.
5. **Taş hiç kaybolmaz.** Yalnız yazı gidip gelir.
6. `prefers-reduced-motion: reduce` → bütün animasyon kapalı; taş 20°'de, yazı tam opak sabit.

## 5. Renkler
- Koyu tema: gradyan yukarıdaki gibi, `--glow: rgba(243,242,239,.45)`
- Açık tema: gradyan `#3A3A3A → #5E5E5E → #C9C6BE → #5E5E5E → #3A3A3A`, `--glow: rgba(20,20,20,.25)`

## 6. Nerede
- **Kabuk (her ekran):** sol üst, menü şeridinin başında: taş 52 px + 14 px boşluk + `MİHENK`; tek `<a>` içinde, Bugün'e gider.
- **Giriş ekranı:** taş 96 px ekranın ortasında, yazı altında ortalı; form 1,6 sn'de belirir, döngüden bağımsız sabit; alanlar animasyon bitmeden de yazılabilir.

## 7. Kabul (Aşkın)
Uygulama ile tuvaldeki pano **yan yana, Play'de** izlenir. Ekran görüntüleri: 0 sn (taş 20°, yazı yok), 2 sn (yazı netleşiyor), 6 sn (ikisi sabit), 9,5 sn (yazı sönüyor, taş duruyor), 14 sn (taş ~200° dönmüş); koyu + açık; kabuk + giriş. Çizgi sayısı, yer, ölçü, renk, saniye bire bir değilse **GEÇTİ sayılmaz** ([[tasarim-birebir-tuval]]). Diğer 8 şemada amblem **değişmez**; ekran görüntüsü karşılaştırmasıyla kanıtlanır.

## 8. Maliyet
SVG + CSS; ek dosya, görsel ya da JavaScript yok. Performans etkisi önemsiz (GPU dönüşümü, 11 sn'de bir süzgeç). Kabuk paketine (TEMA-YILDIZ-PAKET-NOTU § 4 adım D) girer.
