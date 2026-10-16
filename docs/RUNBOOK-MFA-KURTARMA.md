# RUNBOOK — SAHİP kilitlenmesi ve MFA kurtarma

> Paket T-1 (+F-6) · 45-API devir koşulu (1). **Bu belge yazılmadan 45-API
> canlıya alınamaz ve T-1'in gerçek üretim yayın adımı başlayamaz.**
> Tek belge kökü: `docs/` (MÜHÜR 3 / K6).

## Sorun

Bir kiracıda **tek bir SAHİP** kullanıcı olabilir. O kullanıcı MFA'yı açar ve
telefonunu/doğrulayıcı uygulamasını kaybederse, kiracıya **hiç kimse**
giremez: SAHİP rolünü geri verebilecek başka bir SAHİP yoktur.

**Şema gerçeği (koddan doğrulandı):** MFA durumu `kullanici_mfa` tablosunda
durur — `durum` (`BEKLIYOR` | `ETKIN`), `gizli_sifreli` (AES-256-GCM ile
şifreli TOTP gizi, `v1:` önekli). Kullanıcı tablosunda MFA bayrağı **yoktur**.
Kurtarma kodları `mfa_kurtarma_kodlari` tablosunda **SHA-256 hash** olarak
durur; düz kod hiçbir yerde saklanmaz.

## Önlem (müşteriye verilecek uyarı — kurulumda söylenir)

> **MFA'yı açmadan önce kurtarma kodlarınızı yazdırın ve kasada saklayın.**
> Kodlar yalnız bir kez gösterilir; sistem onları hash'leyerek saklar, geri
> gösteremez. Kod da telefon da yoksa kurtarma **sunucuya erişim gerektirir**.

## Akış 1 — Kurtarma kodu elde (tercih edilen)

1. Giriş ekranında ikinci adımda **"Kurtarma kodu kullan"**.
2. Kod tek kullanımlıktır; kullanılan kod **silinmez**, `kullanildi=1` ve
   `kullanildi_tarih` ile işaretlenir (denetim izi bozulmasın).
3. Giriş sonrası **hemen** MFA'yı sıfırlayıp yeni kod seti üretin.

Doğrulama:

```sql
SELECT COUNT(*) AS kalan FROM mfa_kurtarma_kodlari
 WHERE tenant_id = ? AND kullanici_id = ? AND kullanildi = 0;
```

## Akış 2 — Kod da yok: sunucu tarafı kurtarma

> ⚠ Bu yordam **kimlik doğrulamayı atlar**. Kötüye kullanıldığında bir
> saldırgan (ya da veritabanına erişimi olan bir personel) SAHİP hesabını
> ele geçirebilir. Bu yüzden: **yalnız sunucuya fiziksel/SSH erişimi olan
> yetkili**, **müşteri yetkilisinin yazılı talebiyle**, **kayıt tutarak**.

**Kim yetkili:** ByGatech tarafında yalnız Kurucu'nun yazılı onayıyla DevOps
(Batuhan) ya da Güvenlik (Mehmet). Müşteri tarafında işletme sahibi.

### Adımlar

1. **Önce yedek al** — `./dotnet/yedek-al-ve-dogrula.sh`, çıkış kodu 0.
2. Kimliği ve talebi kayda geçir (kim istedi, ne zaman, hangi kanal).
3. Etkilenen kaydı **oku** (henüz değiştirme):

```sql
SELECT m.id, m.kullanici_id, m.durum, m.etkinlesti
  FROM kullanici_mfa m
  JOIN kullanicilar k ON k.id = m.kullanici_id
 WHERE m.tenant_id = ? AND k.eposta = ?;
```

4. MFA'yı **pasife al** (kayıt SİLİNMEZ — denetim izi):

```sql
UPDATE kullanici_mfa
   SET durum = 'BEKLIYOR', son_kullanilan_adim = NULL,
       yanlis_deneme = 0, kilit_bitis = NULL, guncellendi = UTC_TIMESTAMP()
 WHERE tenant_id = ? AND kullanici_id = ?;
```

> `durum='BEKLIYOR'` giriş akışının ikinci adımını **tetiklemez**; kullanıcı
> parolasıyla girer ve MFA'yı yeniden kurar. `gizli_sifreli` alanına
> **DOKUNULMAZ** (eski giz kullanılamaz hale gelmiştir, ama kayıt korunur).

5. **Denetim izine yaz.** Bu işlem uygulama dışından yapıldığı için
   `DenetimServisi` kendiliğinden kayıt üretmez. İz elle eklenir:

```sql
INSERT INTO denetim_izi
  (id, tenant_id, kullanici_id, islem, varlik, varlik_id, aciklama, ip_adresi, olusturuldu)
VALUES (UUID(), ?, ?, 'MFA_SUNUCU_KURTARMA', 'kullanici_mfa', ?,
        'Kurtarma kodu da kayıp; sunucu tarafı MFA sıfırlama. Talep: <kim/ne zaman/kanal>',
        '<yetkili ip>', UTC_TIMESTAMP());
```

> Sütun adlarını uygulamadaki güncel `denetim_izi` şemasına göre doğrulayın
> (`SHOW COLUMNS FROM denetim_izi`); şema değişirse bu örnek değil, **şema**
> esastır.

6. Kullanıcı girer → MFA'yı yeniden kurar → **yeni kurtarma kodlarını yazdırır**.
7. İşlemi kapanış notuyla kayda geçir.

## Kötüye kullanım riski ve azaltma

* Veritabanına erişimi olan herkes bu yordamı uygulayabilir → MySQL 3306
  **dışa kapalıdır** (`docker-compose.uretim.yml`, `ports` yok) ve root
  parolası kuruluma özgüdür (`dotnet/kurulum.sh`).
* Denetim izi **append-only**'dir; kurtarma kaydı sonradan silinemez.
* Yedek her adımdan önce alınır: yanlış satır güncellenirse geri dönülür.

## Doğrulayıcı uygulamalar

Üçüncü taraf doğrulayıcıyla elle doğrulama **45-UI (dalga 2c)** kalemidir ve
T-1'in kabul ölçütü değildir. Bu satır o doğrulama yapıldığında doldurulur:

* **Hangi uygulamalarla test edildi:** _(45-UI'de doldurulacak — bugün BOŞ;
  tahmin yazılmaz, Anayasa md. 1.)_

## Anahtar rotasyonu ile ilişkisi

`KUYUMCU_SIFRELEME_ANAHTARI` değişir de `kullanici_mfa.gizli_sifreli` alanları
yeniden şifrelenmezse **tüm MFA kullanıcıları kilitlenir** ve hepsi için bu
runbook gerekir. Rotasyon yordamı: `docs/DEPLOY.md` § sır rotasyonu.
