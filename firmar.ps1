# =============================================================================
#  firmar.ps1  --  Firma digital de los .exe de FileNet
#
#  Uso:
#     powershell -ExecutionPolicy Bypass -File firmar.ps1
#     powershell -ExecutionPolicy Bypass -File firmar.ps1 "ruta\al.exe"
#
#  Usa el certificado de recursos\firma-autofirmada.pfx (VALIDO SOLO PARA
#  PROBAR EL FLUJO: en otros PCs Windows seguira avisando).
#  Para distribuir de verdad: sustituye ese .pfx por tu certificado OV/EV
#  (o usa Azure Trusted Signing) y cambia $pass por su contrasena.
# =============================================================================
param([string[]]$Ficheros)

$ErrorActionPreference = 'Stop'
$raiz = $PSScriptRoot
$pfx  = Join-Path $raiz "recursos\firma-autofirmada.pfx"
$pass = "filenet"          # <-- contrasena del .pfx

if (-not (Test-Path $pfx)) {
    Write-Host "[firma] No hay certificado en recursos\ -> se omite la firma."
    exit 0
}

if (-not $Ficheros -or $Ficheros.Count -eq 0) {
    $Ficheros = @(Get-ChildItem -Path $raiz -Filter *.exe | Select-Object -ExpandProperty FullName)
}
if ($Ficheros.Count -eq 0) { Write-Host "[firma] No hay .exe que firmar."; exit 0 }

$sec  = ConvertTo-SecureString -String $pass -Force -AsPlainText
$cert = Import-PfxCertificate -FilePath $pfx -CertStoreLocation "Cert:\CurrentUser\My" -Password $sec

$sellos = @("http://timestamp.digicert.com", "http://timestamp.sectigo.com")
foreach ($f in $Ficheros) {
    if (-not (Test-Path $f)) { continue }
    $firmado = $false
    foreach ($sello in $sellos) {
        try {
            $r = Set-AuthenticodeSignature -FilePath $f -Certificate $cert `
                 -HashAlgorithm SHA256 -TimestampServer $sello -ErrorAction Stop
            Write-Host ("[firma] {0}  ->  {1}  (con sello de tiempo)" -f (Split-Path $f -Leaf), $r.Status)
            $firmado = $true
            break
        } catch { }
    }
    if (-not $firmado) {
        try {
            $r = Set-AuthenticodeSignature -FilePath $f -Certificate $cert -HashAlgorithm SHA256 -ErrorAction Stop
            Write-Host ("[firma] {0}  ->  {1}  (sin sello: sin conexion?)" -f (Split-Path $f -Leaf), $r.Status)
        } catch {
            Write-Host ("[firma] {0}  ->  ERROR: {1}" -f (Split-Path $f -Leaf), $_.Exception.Message)
        }
    }
}
