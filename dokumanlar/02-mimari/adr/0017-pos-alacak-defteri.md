# ADR-0017 — POS alacak defteri bir YAN DEFTERDİR

> Durum: KABUL · Tarih: 2026-09-18 · Paket 31 (Faz A + Faz B)
> ⚠ Spesifikasyon bu ADR'ı "0016" diye anıyordu; 0016 numarası **reyon yerleşimi**
> ADR'ına aitti ve kullanımdaydı. Numara 0017'ye kaydırıldı (sapma, gerekçe burada).

## Bağlam
Kart tahsilatı bugün kasaya **brüt** giriyor (`SatisModulu.KasaGuncelle`). Bankanın
kestiği komisyon ve 1–30 günlük valör hiçbir yerde tutulmuyordu; kuyumcu POS
mutabakatını Excel'de yapıyordu. `KasaHareket.KomisyonOran` ve `.BlokajBitis`
alanları vardı ama satış yolunda **hiç yazılmıyordu**.

## Karar
1. POS bilgisi `kasa_hareketleri`'ne kolon olarak **eklenmez**. Dört yeni tablo:
   `pos_cihazlari` · `pos_tahsilatlari` · `pos_taksit_oranlari` · `pos_yatma_eslesmeleri`.
   Gerekçe: bir banka yatması **N** tahsilatı kapatır (N:M); tek tahsilat–tek kasa
   hareketi varsayımı kısmi kapanışı taşıyamaz.
2. `pos_tahsilatlari` bir **YAN DEFTERDİR**: kasa bakiyesine, cari bakiyeye ve
   `hareketler` defterine **hiç dokunmaz**. İkinci bir muhasebe motoru açılmadı;
   `HareketFabrikasi` tek defter kapısı olarak kalır.
3. Tabloya yazan **tek yol** `Modules/Pos/PosDefteri.Ekle`'dir. `db.PosTahsilatlari.Add(`
   deseni tüm ağaçta 1 kez geçer ve `denetim.sh` bunu sayar (Hareketler.Add=1
   kuralının POS karşılığı).
4. `yatan_tutar` ve `durum` **türetilmiş özettir**: yalnız `PosDefteri.Ozetle`,
   `geri_alindi = false` eşleşmelerin toplamından hesaplar. Elle atanmaz.
5. Komisyon oranı ve valör günü **satıra dondurulur** (snapshot). Cihaz ayarı
   sonradan değişse de yazılmış satırlar değişmez — `kur_snapshot` mantığının aynısı.
6. Finansal varlıkta **hard DELETE yoktur**: cihaz pasife alınır, tahsilat
   `durum=IPTAL` + gerekçe (≥20) ile hükümsüz kılınır, eşleşme `geri_alindi=true`
   + gerekçe ile geri alınır (ADR-0004 append-only).

## Sonuçlar
- Kasa bakiyesi hesabı ve gün kapanışı formülü **değişmedi**. Bunun bilinen bedeli:
  kart satışı olan günde gün kapanışı sayım farkı çıkar (canlı ölçüm: −10.000,00 TL
  bir kart satışında). Bu paket **ölçtü, düzeltmedi**; düzeltme paket 30'un konusudur.
- `KasaModulu.HareketGir` komisyon kusuru (bakiyeye net, harekete brüt) **DEVAM
  EDİYOR**: Karar E üç mercekte de bu paketten çıkarıldı, ayrı mühür ister.
- Komisyon oranı ve valör günü **mevzuat değildir** (banka üye işyeri sözleşmesi);
  koda hiçbir sayı gömülmedi, cihaz tanımında kullanıcıdan alınır (ADR-0005).
- Resmî tatil takvimi **yoktur ve uydurulmadı**: `Pos.ValorIsGunu` yalnız haftasonu
  atlar; bayramda banka yatırmayabilir ve sistem bunu bilmez.
