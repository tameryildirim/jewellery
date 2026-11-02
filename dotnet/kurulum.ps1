# ═══════════════════════════════════════════════════════════════════════════
# KUYUMCU ERP — KURULUM SİHİRBAZI / SIR ÜRETİCİ (Windows) — Paket T-1 + F-6
#
# kurulum.sh'ın Windows ikizi. AYNI kurallar:
#   • Sır DEĞERİ ekrana/günlüğe BASILMAZ (yalnız var/yok).
#   • Var olan dosya sessizce EZİLMEZ (-Yenile bayrağı + zaman damgalı kopya).
#   • Rastgelelik System.Security.Cryptography.RandomNumberGenerator'dan gelir
#     (Get-Random KRİPTOGRAFİK DEĞİLDİR, kullanılmaz).
#   • Dosya ACL'i: yalnız SYSTEM, Administrators ve servis hesabı okuyabilir;
#     kalıtım KIRILIR (aksi hâlde Users grubu okur — sır dünyaya açık kalır).
#
# Kullanım (yönetici PowerShell):
#   .\dotnet\kurulum.ps1 -Hedef 'C:\ProgramData\Kuyumcu\.env' `
#                        -Vt kuyumcu_net -TersVekil true [-Yenile] `
#                        [-Adres http://localhost:5199] [-KisayolDizin <dizin>]
#   .\dotnet\kurulum.ps1 -Hedef ... -SadeceKisayol [-Adres <yeni>]
#        → yalnız kısayolu (yeniden) üretir; sırlara DOKUNMAZ.
#
# ADRES VE KISAYOL (PAKET P-33 · K-26 kararı — kurulum.sh ile AYNI kural):
#   Adres kısayola gömülmez, KURULUMDA SORULUR (varsayılan
#   http://localhost:5199); .env'de TEK satırda tutulur (BYGATECH_ADRES) ve
#   masaüstüne "ByGatech.url" kısayolu o adresle üretilir. Adres değişirse
#   -SadeceKisayol -Adres <yeni> satırı günceller ve kısayolu yeniden üretir.
#   Adres bu makineyi gösteriyorsa (localhost/127.0.0.1:PORT) uygulama o
#   portu dinler; değilse ters vekil arkasında 127.0.0.1:5000'i dinler.
# ═══════════════════════════════════════════════════════════════════════════
[CmdletBinding()]
param(
    [string]$Hedef = 'C:\ProgramData\Kuyumcu\.env',
    [string]$Vt = 'kuyumcu_net',
    [ValidateSet('true','false')][string]$TersVekil = 'true',
    [string]$ServisHesabi = 'NT SERVICE\kuyumcu-erp',
    [switch]$Yenile,
    [string]$Adres = '',
    [string]$KisayolDizin = [Environment]::GetFolderPath('Desktop'),
    [switch]$SadeceKisayol
)

$ErrorActionPreference = 'Stop'

function Yeni-Sir([int]$bayt) {
    $tampon = New-Object byte[] $bayt
    [System.Security.Cryptography.RandomNumberGenerator]::Fill($tampon)
    return [Convert]::ToBase64String($tampon)
}

# ── ADRES + KISAYOL (P-33 · K-26) ───────────────────────────────────────────
$VarsayilanAdres = 'http://localhost:5199'

function Adres-Belirle([string]$a) {
    if ([string]::IsNullOrWhiteSpace($a)) {
        if ([Environment]::UserInteractive -and -not [Console]::IsInputRedirected) {
            $a = Read-Host "Uygulamanın adresi (kısayol bu adrese gider) [$VarsayilanAdres]"
        } else {
            Write-Host "  · adres sorulamadı (etkileşimsiz koşu) — varsayılan: $VarsayilanAdres"
        }
        if ([string]::IsNullOrWhiteSpace($a)) { $a = $VarsayilanAdres }
    }
    $a = $a.TrimEnd('/')
    if ($a -notmatch '^https?://[A-Za-z0-9.-]+(:[0-9]{1,5})?$') {
        Write-Error "Geçersiz adres '$a' (örnek: http://localhost:5199)."; exit 2
    }
    return $a
}

function Bag-Adresi([string]$a) {
    if ($a -match '^http://(localhost|127\.0\.0\.1):([0-9]+)$') { return "http://127.0.0.1:$($Matches[2])" }
    return 'http://127.0.0.1:5000'
}

function Kisayol-Uret([string]$a) {
    if (-not (Test-Path $KisayolDizin)) { New-Item -ItemType Directory -Path $KisayolDizin -Force | Out-Null }
    $dosya = Join-Path $KisayolDizin 'ByGatech.url'
    Set-Content -Path $dosya -Value @('[InternetShortcut]', "URL=$a") -Encoding UTF8
    if (-not ((Get-Content $dosya) -contains "URL=$a")) { Write-Error "Kısayol adresi taşımıyor: $dosya"; exit 3 }
    Write-Host "  [OK] kısayol: $dosya -> $a"
}

function Adres-Satirini-Yaz([string]$hedef, [string]$a) {
    $icerik = @(Get-Content $hedef | Where-Object { $_ -notlike 'BYGATECH_ADRES=*' -and $_ -notlike 'ASPNETCORE_URLS=*' -and $_ -notlike 'Urls=*' })
    # "Urls" ETKİN anahtardır (appsettings.Production.json "Urls"unu ezer).
    $icerik += "Urls=$(Bag-Adresi $a)"
    $icerik += "ASPNETCORE_URLS=$(Bag-Adresi $a)"
    $icerik += "BYGATECH_ADRES=$a"
    Set-Content -Path $hedef -Value $icerik -Encoding UTF8
}

if ($SadeceKisayol) {
    if (-not (Test-Path $Hedef)) { Write-Error "'$Hedef' yok; önce kurulum yapılmalı."; exit 2 }
    if ([string]::IsNullOrWhiteSpace($Adres)) {
        $Adres = (Get-Content $Hedef | Where-Object { $_ -like 'BYGATECH_ADRES=*' } | Select-Object -First 1) -replace '^BYGATECH_ADRES=', ''
    }
    $Adres = Adres-Belirle $Adres
    Adres-Satirini-Yaz $Hedef $Adres
    Kisayol-Uret $Adres
    Write-Host "KISAYOL ÜRETİLDİ — sırlara dokunulmadı. Adres: $Adres (satır: BYGATECH_ADRES)"
    exit 0
}

$Adres = Adres-Belirle $Adres

if ((Test-Path $Hedef) -and (-not $Yenile)) {
    Write-Host "DURDU: '$Hedef' zaten var. Sırlar SESSİZCE EZİLMEZ."
    Write-Host "       Yenilemek için -Yenile bayrağını verin."
    Write-Host "       (JWT sırrı değişirse oturumlar düşer; şifreleme anahtarı"
    Write-Host "        değişirse şifreli kimlik alanları okunamaz ve MFA gizleri"
    Write-Host "        yeniden şifrelenmezse TÜM MFA kullanıcıları kilitlenir —"
    Write-Host "        docs/DEPLOY.md § sır rotasyonu.)"
    exit 1
}

if (Test-Path $Hedef) {
    $kopya = "$Hedef.$(Get-Date -Format yyyyMMddHHmmss).yedek"
    Copy-Item $Hedef $kopya
    Write-Host "  [OK] eski dosya kopyalandı: $kopya"
}

$dizin = Split-Path -Parent $Hedef
if (-not (Test-Path $dizin)) { New-Item -ItemType Directory -Path $dizin -Force | Out-Null }

# ── SIR ÜRETİMİ (değerler DEĞİŞKENDE kalır, yazdırılmaz) ───────────────────
$jwt        = (Yeni-Sir 48).Replace('=','')
$dbParola   = (Yeni-Sir 32).Replace('=','').Replace('/','').Replace('+','')
$sifreleme  = Yeni-Sir 32
$yedekAnah  = Yeni-Sir 32

if ($jwt.Length -lt 32 -or $dbParola.Length -lt 16) {
    Write-Error 'Sır üretimi beklenen uzunlukta değil; dosya yazılmadı.'; exit 3
}
if ($sifreleme -eq $yedekAnah) {
    Write-Error 'Yedek anahtarı ile alan şifreleme anahtarı aynı çıktı.'; exit 3
}

$satirlar = @(
    "# ByGatech üretim sırları — $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')",
    '# dotnet/kurulum.ps1 üretti. ELLE DÜZENLEMEYİN, KOPYALAMAYIN, PAYLAŞMAYIN.',
    'ASPNETCORE_ENVIRONMENT=Production',
    "KUYUMCU_VT=$Vt",
    "MYSQL_ROOT_PASSWORD=$dbParola",
    "ConnectionStrings__Kuyumcu=server=127.0.0.1;port=3306;database=$Vt;user=root;password=$dbParola",
    "Jwt__Secret=$jwt",
    "KUYUMCU_SIFRELEME_ANAHTARI=$sifreleme",
    "Yedek__SifrelemeAnahtari=$yedekAnah",
    "Uretim__TersVekilArkasinda=$TersVekil",
    "Urls=$(Bag-Adresi $Adres)",
    "ASPNETCORE_URLS=$(Bag-Adresi $Adres)",
    "BYGATECH_ADRES=$Adres"
)
Set-Content -Path $Hedef -Value $satirlar -Encoding UTF8

# ── ACL: kalıtımı KIR, yalnız SYSTEM + Administrators + servis hesabı ──────
$acl = Get-Acl $Hedef
$acl.SetAccessRuleProtection($true, $false)
foreach ($k in $acl.Access) { [void]$acl.RemoveAccessRule($k) }
foreach ($kimlik in @('NT AUTHORITY\SYSTEM', 'BUILTIN\Administrators', $ServisHesabi)) {
    try {
        $acl.AddAccessRule((New-Object System.Security.AccessControl.FileSystemAccessRule(
            $kimlik, 'Read', 'Allow')))
    } catch {
        Write-Host "  [UYARI] '$kimlik' ACL'e eklenemedi (hesap yok olabilir)."
    }
}
Set-Acl -Path $Hedef -AclObject $acl

# ── ÇIKTI: YALNIZ VAR/YOK ─────────────────────────────────────────────────
Write-Host ''
Write-Host 'KURULUM SIRLARI ÜRETİLDİ — hiçbir değer ekrana basılmadı.'
$icerik = Get-Content $Hedef
foreach ($a in @('ASPNETCORE_ENVIRONMENT','KUYUMCU_VT','MYSQL_ROOT_PASSWORD',
                 'ConnectionStrings__Kuyumcu','Jwt__Secret',
                 'KUYUMCU_SIFRELEME_ANAHTARI','Yedek__SifrelemeAnahtari',
                 'Uretim__TersVekilArkasinda','BYGATECH_ADRES')) {
    if ($icerik | Where-Object { $_ -like "$a=*" }) { Write-Host "  [OK]  $a : var" }
    else { Write-Host "  [YOK] $a" }
}
Write-Host ''
Write-Host "Dosya : $Hedef (ACL kalıtımı kırıldı, yalnız SYSTEM/Administrators/servis okur)"
Kisayol-Uret $Adres
Write-Host "Adres : $Adres (tek satır: BYGATECH_ADRES; değiştirmek için -SadeceKisayol -Adres <yeni>)"
Write-Host 'YEDEKLEYİN: KUYUMCU_SIFRELEME_ANAHTARI kaybolursa şifreli kimlik'
Write-Host 'verileri GERİ OKUNAMAZ (MASAK/KVKK alanları).'
