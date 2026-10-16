# ═══════════════════════════════════════════════════════════════════════════
# KUYUMCU ERP — WINDOWS SERVİS KURULUMU (Paket T-1 + F-6)
#
# start.bat'ın YERİNİ ALIR. start.bat müşteriye .NET SDK + `dotnet run` +
# ASPNETCORE_ENVIRONMENT=Development dayatıyordu; üçü de üretimde YASAKTIR
# (FAIL-CLOSED 6). Burada self-contained yayın çıktısı bir Windows servisi
# olarak kaydedilir; SDK GEREKMEZ.
#
# ÖNKOŞUL:
#   1. .\dotnet\yayin-al.sh win-x64   → dotnet\yayin\win-x64\Kuyumcu.Api.exe
#   2. .\dotnet\kurulum.ps1           → C:\ProgramData\Kuyumcu\.env (sırlar)
#
# Kullanım (yönetici PowerShell):
#   .\deploy\kuyumcu-service-kur.ps1 -YayinDizini 'C:\Kuyumcu\yayin'
# ═══════════════════════════════════════════════════════════════════════════
[CmdletBinding()]
param(
    [string]$YayinDizini = 'C:\Kuyumcu\yayin',
    [string]$ServisAdi = 'kuyumcu-erp',
    [string]$EnvDosyasi = 'C:\ProgramData\Kuyumcu\.env',
    [switch]$Kaldir
)

$ErrorActionPreference = 'Stop'

if ($Kaldir) {
    sc.exe stop $ServisAdi | Out-Null
    sc.exe delete $ServisAdi
    Write-Host "Servis kaldırıldı: $ServisAdi"
    exit 0
}

$exe = Join-Path $YayinDizini 'Kuyumcu.Api.exe'
if (-not (Test-Path $exe)) {
    Write-Error "Yayın çıktısı yok: $exe  (önce dotnet/yayin-al.sh win-x64)"
    exit 2
}
if (-not (Test-Path $EnvDosyasi)) {
    Write-Error "Sır dosyası yok: $EnvDosyasi  (önce dotnet/kurulum.ps1)"
    exit 2
}

# ── SIRLARI MAKİNE KAPSAMINDA ORTAM DEĞİŞKENİ YAP ─────────────────────────
# Windows'ta systemd'nin EnvironmentFile karşılığı yoktur. .env dosyası
# OKUNUR ve makine kapsamlı ortam değişkenlerine yazılır. DEĞERLER EKRANA
# BASILMAZ; yalnız hangi anahtarın ayarlandığı söylenir.
$ayarlandi = @()
foreach ($satir in Get-Content $EnvDosyasi) {
    if ($satir -match '^\s*#' -or $satir -notmatch '=') { continue }
    $ad = $satir.Substring(0, $satir.IndexOf('=')).Trim()
    $deger = $satir.Substring($satir.IndexOf('=') + 1)
    [Environment]::SetEnvironmentVariable($ad, $deger, 'Machine')
    $ayarlandi += $ad
}
# FAIL-CLOSED 6: üretim servisi Production'a SABİTLENİR; .env'deki bir
# Development değeri burada EZİLİR.
[Environment]::SetEnvironmentVariable('ASPNETCORE_ENVIRONMENT', 'Production', 'Machine')
Write-Host "Makine kapsamında ayarlanan anahtarlar (değer basılmadı):"
$ayarlandi | ForEach-Object { Write-Host "  [OK] $_" }

# ── SERVİS ────────────────────────────────────────────────────────────────
# Program.cs'teki builder.Host.UseWindowsService() sayesinde süreç servis
# denetleyicisine doğru yanıt verir (durdurma sinyali, EventLog hedefi).
# K14 (mühür 4): Windows'ta kalıcı günlük EventLog'a yazılır; kaynak adı
# servis adıdır. Saklama süresi/boyut Olay Görüntüleyici'den yapılandırılır.
$varMi = Get-Service -Name $ServisAdi -ErrorAction SilentlyContinue
if ($varMi) {
    Write-Host "Servis zaten var, güncelleniyor: $ServisAdi"
    sc.exe stop $ServisAdi | Out-Null
    sc.exe config $ServisAdi binPath= "`"$exe`"" start= auto | Out-Null
} else {
    sc.exe create $ServisAdi binPath= "`"$exe`"" start= auto `
        DisplayName= "Kuyumcu ERP" obj= "NT SERVICE\$ServisAdi" | Out-Null
}
sc.exe description $ServisAdi "Kuyumcu ERP (API + Blazor WASM, tek surec)" | Out-Null

# Çıkış kodu 3 = bekleyen migration (FAIL-CLOSED 5). Sonsuz yeniden başlatma
# yerine operatör runbook'u çalıştırır: docs/RUNBOOK-SEMA-YUKSELTME.md
sc.exe failure $ServisAdi reset= 86400 actions= restart/5000/restart/10000/none/0 | Out-Null

if (-not (New-Object Security.Principal.WindowsPrincipal(
    [Security.Principal.WindowsIdentity]::GetCurrent())).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Warning 'Yönetici hakkı olmadan servis kaydı başarısız olabilir.'
}

sc.exe start $ServisAdi | Out-Null
Start-Sleep -Seconds 3
$durum = (Get-Service -Name $ServisAdi).Status
Write-Host "Servis durumu: $durum"
if ($durum -ne 'Running') {
    Write-Warning 'Servis çalışmıyor. Olası sebepler (fail-closed, KASITLI):'
    Write-Warning '  • Varsayılan/örnek sır (Jwt__Secret, DB parolası) → kurulum.ps1 çalıştırın'
    Write-Warning '  • TLS beyanı yok → Uretim__TersVekilArkasinda=true ya da https Urls'
    Write-Warning '  • Bekleyen migration (çıkış kodu 3) → docs/RUNBOOK-SEMA-YUKSELTME.md'
    Write-Warning 'Ayrıntı: Olay Görüntüleyici → Uygulama → kaynak kuyumcu-erp'
    exit 1
}
Write-Host ''
Write-Host 'Doğrulama:'
Write-Host '  curl.exe -i http://127.0.0.1:5000/saglik        → 200 Healthy'
Write-Host '  curl.exe -i http://127.0.0.1:5000/saglik/hazir  → 200 hazir:true'
