<#
    Correr en la PC del cliente como Administrador (el .bat lo pide solo), desde la carpeta
    publicada nueva. Sirve para instalar y para actualizar:
      - Primera vez: copia los archivos a C:\SinnersRelatosPOS, crea el servicio y lo arranca.
      - Actualización: detecta dónde quedó instalado el servicio, lo detiene, copia encima los
        archivos nuevos SIN tocar la base de datos (sinnersrelatos.db*) y lo vuelve a arrancar.
    En ambos casos también abre el puerto en el firewall de Windows.

    Uso:  doble clic en instalar-servicio.bat (acepta el aviso de permisos de Windows).
#>
param(
    [string]$NombreServicio = "SinnersRelatosPOS",
    [int]$Puerto = 5278,
    [string]$CarpetaNueva = "C:\SinnersRelatosPOS"
)

$ErrorActionPreference = "Stop"

$actual = [Security.Principal.WindowsIdentity]::GetCurrent()
$esAdmin = (New-Object Security.Principal.WindowsPrincipal $actual).IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)
if (-not $esAdmin) {
    Write-Host "Este script necesita ejecutarse como Administrador (usá instalar-servicio.bat)." -ForegroundColor Red
    exit 1
}

if (-not (Test-Path (Join-Path $PSScriptRoot "SinnersRelatos.Web.exe"))) {
    Write-Host "No se encontró SinnersRelatos.Web.exe en esta carpeta. Corré este script desde la carpeta publicada (la que generó publicar.ps1)." -ForegroundColor Red
    exit 1
}

$servicio = Get-CimInstance Win32_Service -Filter "Name='$NombreServicio'"
if ($servicio) {
    # Se actualiza donde ya está instalado: así no queda una segunda copia ni se pierde su base de datos.
    $destino = Split-Path -Parent $servicio.PathName.Trim('"')
    Write-Host "El servicio '$NombreServicio' ya existe en $destino : deteniéndolo para actualizar..."
    Stop-Service -Name $NombreServicio -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
} else {
    $destino = $CarpetaNueva
}

if ($PSScriptRoot.TrimEnd('\') -ne $destino.TrimEnd('\')) {
    Write-Host "Copiando archivos a $destino ..."
    # La base de datos queda fuera de la copia: ni se pisa ni se borra. Códigos de salida < 8 = OK.
    robocopy $PSScriptRoot $destino /E /R:2 /W:2 /NP /NFL /NDL /NJH /NJS /XF sinnersrelatos.db sinnersrelatos.db-shm sinnersrelatos.db-wal | Out-Null
    if ($LASTEXITCODE -ge 8) {
        throw "No se pudieron copiar los archivos a $destino (robocopy devolvió $LASTEXITCODE)."
    }
}

if (-not $servicio) {
    Write-Host "Creando el servicio '$NombreServicio'..."
    $exe = Join-Path $destino "SinnersRelatos.Web.exe"
    New-Service -Name $NombreServicio `
        -BinaryPathName "`"$exe`"" `
        -DisplayName "Sinners & Relatos POS" `
        -Description "Sistema de punto de venta Sinners & Relatos." `
        -StartupType Automatic | Out-Null

    # Si el proceso llegara a crashear, Windows lo reinicia solo (hasta 3 intentos, esperando 5
    # segundos entre cada uno; el contador de fallos se resetea tras un día sin problemas).
    sc.exe failure $NombreServicio reset= 86400 actions= restart/5000/restart/5000/restart/5000 | Out-Null
}

$reglaFirewall = "Sinners & Relatos POS"
if (-not (Get-NetFirewallRule -DisplayName $reglaFirewall -ErrorAction SilentlyContinue)) {
    New-NetFirewallRule -DisplayName $reglaFirewall -Direction Inbound -Protocol TCP -LocalPort $Puerto -Action Allow | Out-Null
    Write-Host "Regla de firewall creada para el puerto $Puerto."
}

Start-Service -Name $NombreServicio

Write-Host ""
Write-Host "Listo. El servicio '$NombreServicio' quedó corriendo desde $destino y arrancará solo con la PC." -ForegroundColor Green

$ip = (Get-NetIPAddress -AddressFamily IPv4 | Where-Object {
    $_.InterfaceAlias -notmatch "Loopback" -and $_.IPAddress -notlike "169.254.*"
} | Select-Object -First 1).IPAddress

Write-Host "Desde las tablets/celulares conectados al mismo Wi-Fi, entrar a: http://${ip}:${Puerto}" -ForegroundColor Cyan
