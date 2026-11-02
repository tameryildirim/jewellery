# Kuyumcu ERP — .NET Sürümü

TypeScript yığınının (NestJS + React) **tam .NET karşılığı**. Aynı MySQL şeması ve
aynı `/api/v1` sözleşmesiyle çalışır — mevcut veritabanı ve Expo mobil uygulaması
değişiklik olmadan kullanılmaya devam eder.

## Yapı

```
dotnet/
├─ Kuyumcu.sln
├─ Kuyumcu.Core/        Saf domain (has gram, ayarlar) + TR/EN sözlükler
│  └─ I18n/Sozlukler.cs   ← ÜRETİLİR: node scripts/gen-i18n-cs.mjs (elle düzenlemeyin)
├─ Kuyumcu.Api/         ASP.NET Core 8 Web API (Nest sözleşmesi birebir)
│  ├─ Data/               EF Core entity'leri (mevcut şemaya eşleme) + DbContext
│  ├─ Auth/               JWT + SHA-256 refresh rotasyonu + rol filtresi
│  ├─ Common/             Nest formatlı hata middleware'i, ULID
│  └─ Modules/            13 modül — {Modul}Modulu.cs (DTO+Servis+Controller)
└─ Kuyumcu.Web/         Blazor WebAssembly (React sayfalarının birebir portu)
   ├─ Servisler/           ApiIstemci (401→refresh), Oturum, Dil, Bicim
   ├─ Layout/              AnaYerlesim, KenarCubugu, UstBar, DilSecici
   ├─ Bilesenler/          Modal, StatKart, Rozet, bantlar, SVG grafikler
   └─ Pages/               10 sayfa (Giris … Ayarlar)
```

## Çalıştırma (geliştirme)

MySQL çalışıyor olmalı (`docker compose up -d mysql`). Bağlantı + JWT ayarları
`Kuyumcu.Api/appsettings.Development.json` içinde.

```bash
cd dotnet
dotnet run --project Kuyumcu.Api
```

Tek süreç **hem API'yi hem Blazor arayüzünü** sunar; adres `dotnet run`
çıktısında yazar ve `--urls` ile verilir (Swagger: /swagger). Üretimde adres
kurulumda sorulur ve tek satırda tutulur (`BYGATECH_ADRES`, bkz. `docs/DEPLOY.md`
§ 3a — PAKET P-33 / K-26). Eski Node sürümü kaldırılmıştır.

## Üretim yayını

```bash
dotnet publish Kuyumcu.Api -c Release -o yayin
# yayin/ klasörü: Kuyumcu.Api.exe + wwwroot (Blazor) — IIS'e veya doğrudan servise
```

Windows'ta NSSM ile servis yapın veya IIS'te ASP.NET Core Hosting Bundle ile
barındırın (bkz. docs/DEPLOY.md — .NET yolu artık tek süreç olduğundan ayrıca
Node kurulumu GEREKMEZ; IIS ARR proxy'sine de gerek yok, site doğrudan Kestrel'e
ya da In-Process hosting'e bağlanır).

## Sözleşme kanıtı

`scripts/seed.mjs` (Node API'sine karşı yazılmış 104 kayıtlık test verisi
script'i) .NET API'ye karşı **değişiklik olmadan** çalıştı: 104/104 kayıt, 0 hata.
İş mantığı eşdeğerliği doğrulanan örnekler:

| Ölçüm | Node API | .NET API |
|---|---|---|
| Günlük satış (16 işlem) | 840.480,00 TL | 840.480,00 TL |
| Günlük alış (20 işlem) | 4.973.100,00 TL | 4.973.100,00 TL |
| Hareket sayısı | 47 | 47 |
| Net has gram hareketi | 1569,809774 | 1569,809774 |
| Hurda has toplamı | 31,6237 | 31,6237 |
| Hüseyin Arslan bakiyesi | 128.500,00 TL | 128.500,00 TL |
| Nakit Kasa (devir sonrası) | 672.006,20 TL | 672.006,20 TL |

> 12 modülün tamamı ayrıca **sözleşme denetçisi ajanlarından** geçti (Nest
> kaynağıyla satır satır karşılaştırma); 8 modülde 10 sadakat düzeltmesi
> uygulandı — en önemlisi K24 milyem 995→1000 (TS `metal.ts` ile eşitlendi;
> yukarıdaki has gram eşitliğini bu düzeltme sağladı).

## Bilinçli sapmalar (port denetçilerinin notlarından)

1. **Decimal'ler JSON'da sayı** döner (Drizzle string döndürüyordu). İstemciler
   her iki biçimi de okur; Blazor `NumberHandling.AllowReadingFromString` kullanır.
2. **POST durum kodları 200** (Nest bazı uçlarda 201 dönerdi).
3. **Doğrulama hata gövdesi**: alan doğrulama mesajları ASP.NET üretimi; iş kuralı
   hataları (`NotFound/BadRequest/Conflict`) Nest formatı ve Türkçe metinlerle birebir.
4. **PATCH'te açık `null` alan temizlemez** (System.Text.Json'da absent/null ayrımı yok).
5. **Çalıntı refresh token tespiti .NET'te daha sıkı**: iptal edilmiş token yeniden
   kullanılırsa kullanıcının tüm oturumları kapatılır (Nest yalnızca 401 veriyordu).
6. **kurSnapshotId artık tenant doğrulamalı** (Nest doğrulamıyordu — güvenlik artışı).
7. Kur zamanlayıcısı cron yerine 5 dk `Task.Delay` döngüsü; tenant güncellemeleri sıralı.

## Sözlük güncelleme akışı

TS sözlükleri (packages/core/src/i18n) hâlâ tek kaynak:
```bash
pnpm --filter @kuyum/core build && node scripts/gen-i18n-cs.mjs
```
