# RUNBOOK — Ücretsiz pilot daveti

> Paket T-1 (+F-6) · MÜHÜR 4 / K6. Tek belge kökü: `docs/` — ikinci bir belge
> kökü açılmaz. **Bu runbook'un yayın/erişim tarafını T-1 kurar; davet ucunun
> kendisini F-2-min teslim eder.**

## Bugünkü gerçek durum (ölçüldü, tahmin değil)

**GÜNCELLEME 2026-09-17 (F-2-min Faz A):** `/api/v1/sistem/*` uçları artık
KODDADIR ve kayıt ucu davetle kilitlenmiştir (`POST /api/v1/auth/kayit`
davetsiz **403** döner). Aşağıdaki "Davet üretimi" bölümü gerçek koşumdan
doldurulmuştur. Bu paketten ÖNCE geçerli olan durum ve T-1'in kurduğu ağ
kapısı aşağıda olduğu gibi durur:

T-1 bu uçların üretim kapısını **önceden** kurar:

* `deploy/nginx-kuyumcu.conf` içinde `location /api/v1/sistem/` bloğu
  **varsayılan `deny all`** ile gelir; izin listesi kuran kişi tarafından
  doldurulur (K4).
* Neden ağ katmanında: bu uçlar bir **anahtar kullanıcıya bağlı değildir**,
  bu yüzden MFA (45-API) onları **korumaz**. Mehmet'in koşulu: MFA
  tamamlanmadan bu uçlar üretime açılmaz — bu bir **takvim kapısıdır**,
  teknik ikame değil.

## Kurulum tarafı (T-1 — bugün yapılabilir)

1. Yönetim ağınızın IP bloğunu nginx şablonuna ekleyin:

```nginx
location /api/v1/sistem/ {
    allow 127.0.0.1;
    allow 203.0.113.10;      # ← yalnız sizin yönetim adresiniz
    deny all;
    ...
}
```

2. Doğrulayın (izin listesi dışından **403** beklenir):

```bash
sudo nginx -t && sudo systemctl reload nginx
curl -s -o /dev/null -w '%{http_code}\n' https://<alan>/api/v1/sistem/deneme
```

> İzin listesi **boş bırakılırsa** uç dışarıdan erişilemez. Bu kasıtlıdır
> (fail-closed): unutulan bir satır kapıyı **açık** değil, **kapalı** bırakır.

## Davet üretimi (F-2-min Faz A — TESLİM EDİLDİ, 2026-09-17)

Aşağıdaki komutlar ve yanıtlar **gerçek koşumdan** alınmıştır (localhost:5198,
kuyumcu_net; doğrulama sonrası test kiracısı ve daveti silinmiştir).

### 0. Anahtarı ortam değişkenine koyun

Sistem anahtarı **yalnız ortam değişkeninden** okunur
(`Sistem__Anahtari`; T-1 sır hattı). `appsettings.json` içinde yalnız
`DEGISTIR-` yer tutucusu durur ve **bu değerle uçlar kapalıdır**:

```bash
curl -s -i https://<alan>/api/v1/sistem/kiracilar
# HTTP/1.1 503 Service Unavailable
# {"message":"Sistem yönetimi uçları bu kurulumda kapalıdır (Sistem:Anahtari yapılandırılmamış)."}
```

Anahtarı kabuk geçmişine ve `ps` çıktısına düşürmeden okuyun:

```bash
read -rs SISTEM_ANAHTARI        # ekrana basmaz, geçmişe yazmaz
export SISTEM_ANAHTARI
```

> Anahtar **32 karakterden kısa olamaz** ve `DEGISTIR` ile başlayamaz;
> ikisinde de uç **kapalı** kalır (fail-closed).

### 1. Davet üret (ham jeton YALNIZ BİR KEZ döner)

`X-Sistem-Operator` başlığı **zorunludur** (en az 3 karakter) ve gövdedeki
`gerekce` alanı **en az 20 karakter** olmalıdır: kim çağırdı, neden çağırdı —
ikisi de denetim izine yazılır. Başlıksız istek **400**, gerekçesiz istek
**400** döner. (Ham jeton ve sistem anahtarı ize ASLA yazılmaz.)

```bash
curl -s -X POST https://<alan>/api/v1/sistem/davetler \
  -H "X-Sistem-Anahtari: $SISTEM_ANAHTARI" \
  -H "X-Sistem-Operator: <adiniz>" \
  -H 'Content-Type: application/json' \
  -d '{"email":"pilot@ornek.test","isletmeAdi":"Örnek Kuyumculuk","denemeGun":14,
       "not":"pilot-1","gerekce":"Fuarda tanisilan pilot kuyumcu icin deneme daveti"}'
```

Gerçek yanıt (kısaltılmadı, jeton örnektir):

```json
{"id":"01M2RFYZ53KDT81YPNG2JY2B3K",
 "jeton":"5lu0E--GbCuz9aa6HH1gMRC-Yfg2soUg_FMQRb-sTv0",
 "jetonUyarisi":"Bu jeton bir daha gösterilmez; güvenli bir kanalla iletin.",
 "denemeGun":14,"gecerlilik":"2026-09-20T20:12:08Z",
 "email":"p***@o***.test","isletmeAdi":"Örnek Kuyumculuk","durum":"BEKLIYOR"}
```

* **Ham jeton sunucuda SAKLANMAZ**: veritabanında yalnız SHA-256 özeti durur.
  Bu yanıtı kaybederseniz jeton kurtarılamaz — **yeni davet üretin**.
* Listede (`GET /api/v1/sistem/davetler`) jeton **dönmez**, e-posta
  **maskelenir** (`p***@o***.test`).
* `gerekce` ZORUNLUDUR (≥20 karakter); diğer alanlar isteğe bağlıdır. `denemeGun` boşsa
  `Sistem:VarsayilanDenemeGun`, `gecerlilikSaat` boşsa
  `Sistem:DavetGecerlilikSaat` geçerlidir.

### 2. Daveti iptal et / iptali geri al

```bash
curl -s -X PATCH https://<alan>/api/v1/sistem/davetler/<id> \
  -H "X-Sistem-Anahtari: $SISTEM_ANAHTARI" -H "X-Sistem-Operator: <adiniz>" \
  -H 'Content-Type: application/json' \
  -d '{"iptal":true,"gerekce":"Pilot vazgecti - davet guvenlik geregi iptal"}'
# iptali geri almak için: {"iptal":false,"gerekce":"Pilot geri dondu - davet yeniden acildi"}
```

Gövdede **göndermediğiniz alan korunur** (not, süre, geçerlilik); `gerekce`
her istekte zorunludur ve yalnız denetim izine yazılır (davet kaydına değil).
Kullanılmış davet değiştirilemez (**400**); o kiracı artık
`/api/v1/sistem/kiracilar` ucundan yönetilir.

### 3. Kiracıları görün, süre uzatın

```bash
curl -s https://<alan>/api/v1/sistem/kiracilar -H "X-Sistem-Anahtari: $SISTEM_ANAHTARI"

curl -s -X PATCH https://<alan>/api/v1/sistem/kiracilar/<tenantId> \
  -H "X-Sistem-Anahtari: $SISTEM_ANAHTARI" -H "X-Sistem-Operator: <adiniz>" \
  -H 'Content-Type: application/json' \
  -d '{"denemeBitis":"2026-10-17T00:00:00Z","gerekce":"Pilot denemesi 30 gun uzatildi - satis talebi"}'
```

* **Gerekçe en az 20 karakterdir** ve denetim izine yazılır (kim · neden ·
  hangi alanlar · istek kimliği · IP). Anahtarın **değeri** ize yazılmaz.
* `durum` alanı ile `DENEME | AKTIF | SALT_OKUNUR` arasında geçiş yapılır;
  `aktif:false` kiracıyı pasife alır. **Kiracı silinmez.**
* Uzatma **anında** etkir (30 sn'lik durum önbelleği açıkça boşaltılır).

### 4. Deneme bitince ne olur

| İşlem | Deneme sürüyor | Süre doldu / SALT_OKUNUR |
|---|---|---|
| Okuma (GET) | açık | **açık** |
| Dışa aktarım (`/api/v1/disa-aktarim`) | açık | **açık** |
| Giriş (`/api/v1/auth/*`) | açık | **açık** |
| Yazma (POST/PUT/PATCH/DELETE) | açık | **403** |

> Veri **rehin alınmaz** (KVKK md. 11-12 · VUK 253/256): süresi dolan kiracı
> giriş yapıp tüm verisini şifreli arşiv olarak indirebilir.

### Sır disiplini

* Sistem anahtarı ortam değişkeninden okunur, komut satırına açık yazılmaz
  (`ps` çıktısına düşer), ekrana basılmaz, loga ve denetim kaydına girmez.
* **SMTP yoktur:** davet e-posta ile **gönderilmez**; metin kopyalanıp
  SMS/WhatsApp ile iletilir.

## Davet metni şablonu (kopyalanabilir)

```
Merhaba <ad>,

Kuyumcu ERP ücretsiz pilot hesabınız hazır. Aşağıdaki bağlantıdan
kendi şifrenizi belirleyerek girebilirsiniz:

Kayıt ekranı: https://<alan>/giris  →  "İşletme Kur" sekmesi
Davet kodunuz: <jeton>

Kod <süre> geçerlidir ve tek kullanımlıktır.
Sorunuz olursa bu numaradan yazabilirsiniz.

ByGatech
```

> **Fiyat/teklif/tahsilat içeren hiçbir metin üretilmez** (MÜHÜR 2: ürün,
> dalga 2b+2c ve QA turu 2 bitene dek yalnız ÜCRETSİZ PİLOT).
