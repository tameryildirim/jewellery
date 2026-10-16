-- ═════════════════════════════════════════════════════════════════════════════
-- PAKET OLCEK-YUK — hacim tohumlaması (YALNIZ kuyumcu_test_yuk; kuyumcu_net'e ASLA)
--
-- Önkoşul: yuk-kiraci-kur.mjs her kiracıyı ürünün uçlarıyla kurdu (5 şablon ürün,
-- 3 cari, 2 gerçek satış fişi). Bu betik o şablonları KLONLAR:
--   büyük kiracı (K001–K003): 50.000 ürün · 180.000 hareket / 1.095 gün · 5.000 cari
--   olağan kiracı (K004–K100): 2.000 ürün · 6.000 hareket / 1.095 gün · 300 cari
-- Defter dışı klon satırları yalnız OKUMA yükü içindir; tutarlılık iddiası taşımaz.
--
-- ⚠ Disk kuralı: binlog'a yazılmaz (SET sql_log_bin=0 — yalnız bu oturum).
-- Çalıştırma: docker exec -i kuyumcu-mysql mysql -uroot -p"$P" kuyumcu_test_yuk < yuk-tohum.sql
-- ═════════════════════════════════════════════════════════════════════════════
SET SESSION sql_log_bin = 0;
SET SESSION cte_max_recursion_depth = 300000;
SELECT IF(DATABASE() = 'kuyumcu_test_yuk', 'DB_DOGRU', 'YANLIS_DB_DUR') AS kapi;
-- Yanlış DB'de en ağır adım hiç koşmasın diye her INSERT bu koşulu da taşır.

DROP TABLE IF EXISTS yuk_sayi;
CREATE TABLE yuk_sayi (k INT PRIMARY KEY);
INSERT INTO yuk_sayi
  WITH RECURSIVE s(k) AS (SELECT 0 UNION ALL SELECT k + 1 FROM s WHERE k < 199999) SELECT k FROM s;

DROP TABLE IF EXISTS yuk_plan;
CREATE TABLE yuk_plan (tenant_id VARCHAR(36) PRIMARY KEY, no CHAR(3), n_urun INT, n_hareket INT, n_cari INT);
INSERT INTO yuk_plan
  SELECT t.id, RIGHT(t.ad, 3),
         IF(RIGHT(t.ad, 3) <= '003', 50000, 2000),
         IF(RIGHT(t.ad, 3) <= '003', 180000, 6000),
         IF(RIGHT(t.ad, 3) <= '003', 5000, 300)
  FROM tenants t WHERE t.ad LIKE 'PAKET-OLCEK-YUK-TEST-K%' AND DATABASE() = 'kuyumcu_test_yuk';

DROP TABLE IF EXISTS yuk_sablon_urun;
CREATE TABLE yuk_sablon_urun (tenant_id VARCHAR(36), r INT, id VARCHAR(36), PRIMARY KEY (tenant_id, r));
INSERT INTO yuk_sablon_urun
  SELECT tenant_id, ROW_NUMBER() OVER (PARTITION BY tenant_id ORDER BY kod) - 1, id
  FROM urunler WHERE tenant_id IN (SELECT tenant_id FROM yuk_plan) AND kod LIKE 'S%';

-- 1) Ürünler
INSERT INTO urunler (id, tenant_id, tip, kod, ad, aciklama, ayar_id, milyem, gram_agirlik, tas_agirlik, has_grami,
  iscilik_tl, satis_fiyati, barkod, rfid, gorsel_url, aktif, olusturuldu, guncellendi, parite_kodu, kmts_kodu,
  tas_ibaresi, tas_kokeni, kategori_id, konsinye, konsinye_birim_bedel, konsinye_cari_id, reyon_id, birim,
  pirlanta_berraklik, pirlanta_karat, pirlanta_kesim, pirlanta_renk, sertifika_kurumu, sertifika_no, rodyum_kaplama,
  alis_maliyeti, hedef_marj, kdv_orani, min_stok, olcu, seri_no, tedarikci_id)
SELECT CONCAT('OYU', p.no, LPAD(s.k, 7, '0')), u.tenant_id, u.tip, CONCAT('OY', p.no, '-', s.k), CONCAT(u.ad, ' #', s.k),
  u.aciklama, u.ayar_id, u.milyem, u.gram_agirlik, u.tas_agirlik, u.has_grami, u.iscilik_tl, u.satis_fiyati, NULL, NULL,
  NULL, u.aktif, NOW(6) - INTERVAL (s.k % 1095) DAY, NOW(6), NULL, u.kmts_kodu, u.tas_ibaresi, u.tas_kokeni,
  u.kategori_id, u.konsinye, u.konsinye_birim_bedel, u.konsinye_cari_id, u.reyon_id, u.birim, u.pirlanta_berraklik,
  u.pirlanta_karat, u.pirlanta_kesim, u.pirlanta_renk, u.sertifika_kurumu, u.sertifika_no, u.rodyum_kaplama,
  u.alis_maliyeti, u.hedef_marj, u.kdv_orani, u.min_stok, u.olcu, u.seri_no, u.tedarikci_id
FROM yuk_plan p JOIN yuk_sayi s ON s.k < p.n_urun
JOIN yuk_sablon_urun su ON su.tenant_id = p.tenant_id AND su.r = s.k % 5
JOIN urunler u ON u.id = su.id
WHERE DATABASE() = 'kuyumcu_test_yuk';
SELECT 'urunler', ROW_COUNT();

-- 2) Stoklar (ürün başına bir şube satırı)
INSERT INTO stoklar (id, tenant_id, sube_id, urun_id, adet, toplam_has_gram, guncellendi, bloke_adet)
SELECT CONCAT('OYS', p.no, LPAD(s.k, 7, '0')), p.tenant_id, st.sube_id, CONCAT('OYU', p.no, LPAD(s.k, 7, '0')),
  1 + (s.k % 7), (1 + (s.k % 7)) * u.has_grami, NOW(6), 0
FROM yuk_plan p JOIN yuk_sayi s ON s.k < p.n_urun
JOIN yuk_sablon_urun su ON su.tenant_id = p.tenant_id AND su.r = s.k % 5
JOIN urunler u ON u.id = su.id
JOIN stoklar st ON st.urun_id = su.id AND st.tenant_id = p.tenant_id
WHERE DATABASE() = 'kuyumcu_test_yuk';
SELECT 'stoklar', ROW_COUNT();

-- 3) Stok partileri (giriş tarihi 3 yıla yayılır → yaşlandırma kovaları dolar)
INSERT INTO stok_partileri (id, tenant_id, sube_id, urun_id, giris_hareket_id, kaynak, adet_giren, adet_kalan,
  has_gram_birim, birim_maliyet_tl, birim_maliyet_has, kur_snapshot_id, giris_tarihi, olusturuldu)
SELECT CONCAT('OYP', p.no, LPAD(s.k, 7, '0')), p.tenant_id, sp.sube_id, CONCAT('OYU', p.no, LPAD(s.k, 7, '0')),
  sp.giris_hareket_id, sp.kaynak, 1 + (s.k % 7), 1 + (s.k % 7), sp.has_gram_birim, sp.birim_maliyet_tl,
  sp.birim_maliyet_has, sp.kur_snapshot_id, NOW(6) - INTERVAL (s.k % 1095) DAY, NOW(6)
FROM yuk_plan p JOIN yuk_sayi s ON s.k < p.n_urun
JOIN yuk_sablon_urun su ON su.tenant_id = p.tenant_id AND su.r = s.k % 5
JOIN stok_partileri sp ON sp.urun_id = su.id AND sp.tenant_id = p.tenant_id
WHERE DATABASE() = 'kuyumcu_test_yuk';
SELECT 'stok_partileri', ROW_COUNT();

-- 4) Cariler
INSERT INTO cariler (id, tenant_id, tur, ad, telefon, email, vergi_no, adres, notlar, kredi_limiti_tl, aktif,
  olusturuldu, guncellendi, varsayilan_vade_gun, fiyat_listesi_id, pep_beyan_tarihi, pep_beyani, pep_gorev,
  dogum_tarihi, iskonto_yuzde, kimlik_turu, segment, unvan, uyruk, hesap_kodu, sistem_carisi)
SELECT CONCAT('OYC', p.no, LPAD(s.k, 7, '0')), p.tenant_id, c.tur, CONCAT('Yük Müşteri ', p.no, '-', s.k),
  CONCAT('0533', LPAD(s.k, 7, '0')), NULL, NULL, c.adres, NULL, c.kredi_limiti_tl, 1, NOW(6), NOW(6),
  c.varsayilan_vade_gun, NULL, NULL, c.pep_beyani, NULL, NULL, c.iskonto_yuzde, c.kimlik_turu, c.segment, NULL,
  c.uyruk, NULL, 0
FROM yuk_plan p JOIN yuk_sayi s ON s.k < p.n_cari
JOIN (SELECT tenant_id, MIN(id) id FROM cariler WHERE ad LIKE 'Müşteri %' GROUP BY tenant_id) cm ON cm.tenant_id = p.tenant_id
JOIN cariler c ON c.id = cm.id
WHERE DATABASE() = 'kuyumcu_test_yuk';
SELECT 'cariler', ROW_COUNT();

-- 5) Hareketler (SATIS şablonundan; %20'si cari bağlı; 1.095 güne yayılır)
INSERT INTO hareketler (id, tenant_id, sube_id, tur, tarih, aciklama, tl_tutari, para_birimi, has_grami, odeme_turu,
  kur_snapshot_id, cari_id, urun_id, karsi_sube_id, kullanici_id, fatura_no, olusturuldu, kmts_kodu, kategori_id,
  konsinye, konsinye_birim_bedel, konsinye_cari_id, reyon_id, nakit_tutar, veresiye_tutar, fis_id,
  iade_kaynak_hareket_id, vade_tarihi, hediye, indirim_tutari, kampanya_id, liste_fiyati, storno_kaynak_id, maden,
  doviz_tutar, adet, personel_id, tahlil_kaynak_hareket_id, siparis_id, takas_eski_has_grami, masak_olcu_tl)
SELECT CONCAT('OYH', p.no, LPAD(s.k, 7, '0')), p.tenant_id, h.sube_id, h.tur,
  NOW(6) - INTERVAL (s.k % 1095) DAY - INTERVAL (s.k % 600) MINUTE, h.aciklama,
  h.tl_tutari + (s.k % 50) * 100, h.para_birimi, h.has_grami, h.odeme_turu, h.kur_snapshot_id,
  IF(s.k % 5 = 0, CONCAT('OYC', p.no, LPAD(s.k % p.n_cari, 7, '0')), NULL),
  CONCAT('OYU', p.no, LPAD(s.k % p.n_urun, 7, '0')), NULL, h.kullanici_id, NULL, NOW(6), h.kmts_kodu, h.kategori_id,
  h.konsinye, h.konsinye_birim_bedel, h.konsinye_cari_id, h.reyon_id, h.nakit_tutar, h.veresiye_tutar, NULL,
  NULL, NULL, h.hediye, h.indirim_tutari, NULL, h.liste_fiyati, NULL, h.maden, h.doviz_tutar, h.adet, h.personel_id,
  NULL, NULL, h.takas_eski_has_grami, h.masak_olcu_tl
FROM yuk_plan p JOIN yuk_sayi s ON s.k < p.n_hareket
JOIN (SELECT tenant_id, MIN(id) id FROM hareketler WHERE tur = 'SATIS' GROUP BY tenant_id) hm ON hm.tenant_id = p.tenant_id
JOIN hareketler h ON h.id = hm.id
WHERE DATABASE() = 'kuyumcu_test_yuk';
SELECT 'hareketler', ROW_COUNT();

-- 6) Kasa hareketleri (her nakit satış klonuna bir kasa bacağı)
INSERT INTO kasa_hareketleri (id, tenant_id, kasa_id, hareket_id, tutar, para_birimi, odeme_turu, komisyon_oran,
  blokaj_bitis, tarih, olusturuldu, aciklama, storno_kaynak_id, gider_kategori_id, karsi_bacak_id)
SELECT CONCAT('OYK', p.no, LPAD(s.k, 7, '0')), p.tenant_id, kh.kasa_id, CONCAT('OYH', p.no, LPAD(s.k, 7, '0')),
  kh.tutar + (s.k % 50) * 100, kh.para_birimi, kh.odeme_turu, kh.komisyon_oran, NULL,
  NOW(6) - INTERVAL (s.k % 1095) DAY - INTERVAL (s.k % 600) MINUTE, NOW(6), kh.aciklama, NULL, NULL, NULL
FROM yuk_plan p JOIN yuk_sayi s ON s.k < p.n_hareket
JOIN (SELECT tenant_id, MIN(id) id FROM kasa_hareketleri GROUP BY tenant_id) km ON km.tenant_id = p.tenant_id
JOIN kasa_hareketleri kh ON kh.id = km.id
WHERE DATABASE() = 'kuyumcu_test_yuk';
SELECT 'kasa_hareketleri', ROW_COUNT();

ANALYZE TABLE urunler, stoklar, stok_partileri, cariler, hareketler, kasa_hareketleri;
SELECT ROUND(SUM(data_length + index_length) / 1048576) AS db_mb
FROM information_schema.tables WHERE table_schema = 'kuyumcu_test_yuk';
