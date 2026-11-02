# Runbook — Yedek ve doğrulanmış geri yükleme (Paket T-3 Faz A)

> **Yer:** runbook'lar YALNIZ `docs/` altındadır (MÜHÜR 3 — tek yayın yolu).
> **Betik:** yedek yolu YALNIZ `dotnet/yedek-al-ve-dogrula.sh`'tır; ortak işler
> `dotnet/yedek-ortak.sh` kitaplığındadır. İkinci bir yedek betiği yoktur.

## 0. Bir cümlede

"Yedek alındı" bir iddia, "geri yüklendi ve sağlaması tuttu" bir kanıttır.
Bu runbook kanıtı nasıl ürettiğimizi ve felaket anında ne yapacağımızı anlatır.

## 1. Ne çalışıyor

| Birim | Ne yapar | Ne zaman |
|---|---|---|
| `kuyumcu-yedek-tam.timer` | Şifreli TAM yedek + dış kopya + saklama | 03:15 ve 09/15/21:15 (Europe/Istanbul) |
| `kuyumcu-yedek-talep.timer` | Uygulamadan gelen **elle yedek** kuyruğunu tüketir | 60 saniyede bir |
| `kuyumcu-yedek-tatbikat.timer` | Geri yükleme **tatbikatı** (ayrı şema + CHECKSUM) | Ayın 1'i 04:30 |

Uygulama **hiçbir koşulda** komut çalıştırmaz: "Elle yedek al" düğmesi
`yedek_talepleri` tablosuna bir satır yazar, yedeği yukarıdaki birim alır.

## 2. Sırlar — nerede durur, nerede DURMAZ

| Sır | Nerede | Not |
|---|---|---|
| `KUYUMCU_DB_SIFRE` | `/etc/kuyumcu/.env` (chmod 600, sahip `kuyumcu`) | Betiğe `--defaults-extra-file` ile verilir; **ARGV'ye girmez** |
| `Yedek__SifrelemeAnahtari` | `/etc/kuyumcu/.env` | **Alan şifreleme anahtarından AYRIDIR**; aynısı verilirse uygulama açılmaz |
| `KUYUMCU_SIFRELEME_ANAHTARI` | `/etc/kuyumcu/.env` | Alan şifreleme (MASAK/KVKK kimlik alanları) |

> ⚠ **ANAHTAR KAYBI = TÜM YEDEKLERİN KAYBI.** Yedek şifreleme anahtarının
> sunucu **dışında** (kasa, parola yöneticisi, ikinci lokasyon) bir kopyası
> bulunmalıdır. Anahtar yedek dosyalarıyla **aynı diskte/dizinde durmaz**.
> Anahtarın sunucu dışı saklama yeri ve kayıp hâlinde kurtarma yolu
> **Kurucu kalemidir** (KURUCUYA-KARARLAR md. 13) ve bu runbook'a yazılana
> kadar T-3 kapanmış sayılmaz.

**Parola argv'de görünmüyor — kanıt:** yedek koşarken alınan `ps aux`
örneğinde (7.201 satır, `mysqldump` 7 kez, `gpg` 7 kez görünürken) veritabanı
parolası ve yedek anahtarı **0 kez** geçti (ölçüm 2026-09-17; ölçümü yapan
kabuğun kendi komut satırı hariç tutuldu).

```bash
# Kanıtı yeniden üretmek için (sırlar dosya içinde atanır, komut satırında değil):
sudo -u kuyumcu /opt/kuyumcu/dotnet/yedek-al-ve-dogrula.sh tam &
for i in $(seq 1 12); do ps aux; done > /tmp/ps-ornek.txt
grep -c -- "--password=" /tmp/ps-ornek.txt   # beklenen: 0
```

## 3. Günlük kullanım

```bash
./dotnet/yedek-al-ve-dogrula.sh tam       ; echo "cikis: $?"   # 0 = yedek + dış kopya tamam
./dotnet/yedek-al-ve-dogrula.sh tatbikat  ; echo "cikis: $?"   # 0 = geri yükleme DOĞRULANDI
./dotnet/yedek-al-ve-dogrula.sh           ; echo "cikis: $?"   # ikisi peş peşe
systemctl list-timers 'kuyumcu-yedek-*'
journalctl -u kuyumcu-yedek-tam -n 50
```

## 4. Fail-closed kapılar (hepsi ölçüldü)

| # | Kural | Davranış |
|---|---|---|
| 1 | CHECKSUM uyuşmazlığı | Koşu `dogrulama=UYUSMADI`, çıkış ≠ 0, dosya saklamadan muaf |
| 2 | Anahtar yok / 32 karakterden kısa / alan anahtarıyla aynı | Yedek **hiç alınmaz**, dosya üretilmez, çıkış 1 |
| 3 | Dış kopya alınamadı (hedef tanımsız da dâhil) | Koşu `BASARISIZ` — tek kopya yedek, yedek değildir |
| 4 | Son 30 günde doğrulanmış yedek yok | Saklama temizliği **hiçbir dosyayı silmez**, yalnız listeler |

## 5. Tatbikat nasıl okunur

Tatbikat, yedeği `kuyumcu_net_tatbikat` şemasına **ham** geri yükler
(`dotnet ef database update` KULLANILMAZ — ikinci migration yolu doğmaz),
tablo sağlama toplamlarını karşılaştırır, sonucu `yedek_tatbikatlari`na yazar
ve şemayı **düşürür** (`sema_dusuruldu=1` bunun kanıtıdır). Kaynak şemaya
hiçbir aşamada yazılmaz.

**Değişken tablo ayrımı (dürüstlük notu):** kaynak parmak izi dump'tan sonra,
yaşayan veritabanından alınır. Dump anı ile karşılaştırma anı arasında yazılan
tablolar (ör. `denetim_kayitlari`, `kur_snapshots`) doğal olarak uyuşmaz.
Betik bunu `information_schema.UPDATE_TIME` ile **ölçerek** ayırır: dump
başlangıcından sonra yazılmışsa "değişken", yazılmamışsa **gerçek uyuşmazlık**
sayılır ve koşu başarısız olur. Yanlış YEŞİL üretmeyen yön budur.

Rapor alanına **yalnız tablo adları** yazılır; hiçbir satır/alan değeri yazılmaz
(yedek kişisel veri taşır — KVKK md. 12/1).

## 6. FELAKET — üretime geri yükleme (insan eliyle)

> Uygulama içinden geri yükleme **yoktur** ve açılmayacaktır (yıkıcı işlem).

1. **Servisi durdur:** `sudo systemctl stop kuyumcu`
2. **Dosyayı seç:** `ls -lt /var/lib/kuyumcu/yedek/*.sql.gz.gpg | head`
   (dış kopyadan getiriyorsan önce yerel diske indir)
3. **Sağlamayı doğrula:** `sha256sum <dosya>` → `yedek_kayitlari.sha256` ile aynı olmalı
4. **Önce AYRI şemaya yükle ve doğrula:** `./dotnet/yedek-al-ve-dogrula.sh tatbikat`
5. **Üretim şemasına al** (yalnız 4. adım temizse):
   ```bash
   gpg --batch --decrypt --passphrase-fd 3 3< /etc/kuyumcu/.yedek-anahtari \
     < <dosya> | gunzip | mysql --defaults-extra-file=/etc/kuyumcu/.my.cnf
   ```
6. **Şema sürümünü kontrol et:** uygulamayı başlat; bekleyen migration varsa
   çıkış kodu **3** ile durur → `docs/RUNBOOK-SEMA-YUKSELTME.md`.
7. **Servisi başlat:** `sudo systemctl start kuyumcu` · `journalctl -u kuyumcu -f`
8. **Kapanış:** `/yedekler` ekranından son yedek ve doğrulama durumunu teyit et.

## 7. RPO / RTO — taahhüt ile ölçüm ayrı satırlardır

| Kavram | Değer | Kaynak |
|---|---|---|
| RPO **taahhüdü** | 360 dk (6 saat) | `appsettings.json` → `Yedek:RpoDakika` |
| RTO **taahhüdü** | 120 dk (2 saat) | `appsettings.json` → `Yedek:RtoDakika` |
| RPO **ölçümü** | son başarılı yedekten bu yana geçen dakika | `/yedekler` ekranı (hesaplanır) |
| RTO **ölçümü** | son tatbikatın süresi | `yedek_tatbikatlari.rto_dakika_olculen` |

Taahhüt bir hedeftir, ölçüm bir gerçektir; ikisi ekranda ve raporda **ayrı**
etiketlerle durur. Ölçülen değer taahhüdü aşarsa bulgu açılır.

## 8. Saklama süresi — yanlış anlaşılmasın

Yedekler **30 gün dönüşümlüdür**. Bu bir **kayıt saklama süresi değildir**:
defter ve belge verisi VUK md. 253 (5 yıl) ve TTK md. 82 (10 yıl) boyunca
**üretim veritabanında** durur. Müşteriye "verinizi 30 gün saklıyoruz"
**denmez**; doğru cümle "yedeklerimizi 30 gün dönüşümlü tutuyoruz"dur.

## 9. Bu pakette KAPSAM DIŞI

- Point-in-time recovery (binlog/PITR) — Faz A'da **yoktur** (MÜHÜR 6 / K1).
  `log_bin` durumu yalnız ölçülür; PITR 1.1 sürümüne bırakıldı.
- Uygulama içinden geri yükleme tetikleme, yedek dosyası indirme ucu.
- Kiracı bazlı kısmi yedek (veri taşınabilirliği `/veri-disa-aktarim`'dadır).
- Çoklu bölge / replica / failover.

## 10. Tatbikat kaydı (PAKET P-33 · BK-30 · Köprü K-5)

Geri yükleme tatbikatı **en geç 30 günde bir** koşar. Son başarılı tatbikatın
tarihi `/yedekler` ekranındaki **"Son geri yükleme tatbikatı"** kartında görünür;
30 günü aşarsa (ya da hiç yapılmadıysa) kart kırmızıya döner ve ekran uyarı bandı
basar. Sınır yapılandırmayla (`Yedek:TatbikatAzamiGun`) yalnız **sıkılaştırılabilir**
(1–30); 30'un üstü 30'a indirilir. Başarısız tatbikat tazelik **sayılmaz**.

`dotnet/denetim.sh` → `YEDEK_TATBIKAT_BAYAT` aşağıdaki `SON_TATBIKAT:` satırını ve
(kap ayaktaysa) `kuyumcu_net.yedek_tatbikatlari`'ndaki son BAŞARILI satırı ölçer;
biri 30 günü aşarsa denetim KIRAR. Her tatbikattan sonra satır güncellenir.

SON_TATBIKAT: 2026-10-24 · BASARILI · 107/107 tablo · gerçek uyuşmazlık 0 · kayıt t202610241518561164 · kaynak kuyumcu_net (geliştirme/QA veritabanı, yerel kap)

| Tarih (UTC) | Veritabanı | Komut | Sonuç | Kanıt |
|---|---|---|---|---|
| 2026-09-23 19:14–19:15 | `kuyumcu_net` (kap `kuyumcu-mysql`) | `yedek-al-ve-dogrula.sh` (tam + tatbikat) | **BAŞARILI** — 76/76 tablo CHECKSUM eşleşti; 1 tablo (`yedek_kayitlari`) dump sonrası yazıldığı için "değişken" sayıldı; tatbikat şeması düşürüldü; 36 sn | `yedek_tatbikatlari.id = t202609231915063497` · `yedek_kayitlari.id = y202609231914272146` (DOGRULANDI, şifreli, dış kopya KOPYALANDI) |
| 2026-09-23 19:29–19:30 | **temiz kurulum** `kuyumcu_net` (boş kap `bygatech-tatbikat-mysql`, 70 migration) | `yedek-al-ve-dogrula.sh` (KUYUMCU_KAP=tatbikat kabı) | **BAŞARILI** — 76/76 tablo; 16 sn; kap tatbikat sonunda silindi | `t202609231929549543` · ayrıntı `docs/RUNBOOK-TESLIMAT.md` § 5 |
| 2026-10-24 18:18 | `kuyumcu_net` (kap `kuyumcu-mysql`) | `yedek-al-ve-dogrula.sh` (tam + tatbikat) | **BAŞARILI** — 107/107 tablo CHECKSUM eşleşti; 3 tablo (`banka_hesaplari`, `denetim_kayitlari`, `yedek_kayitlari`) dump sonrası yazıldığı için "değişken"; tatbikat şeması düşürüldü; 19 sn | `yedek_tatbikatlari.id = t202610241518561164` |

> ⚠ Bu tatbikat **YEREL KAP TATBİKATI**dır (geliştirme makinesindeki MySQL kabı).
> Bulut/üretim sunucusunda koşmuş bir tatbikat **değildir** — "üretim kanıtı"
> denmez (K-6 bulut hesabı ve K-9 alan adı/TLS kurucu kalemleri gelmedi).
> İlk pilot müşteri kurulumunda tatbikat o sunucuda **ayrıca** koşulur ve bu
> tabloya satır olarak eklenir (BK-30).
